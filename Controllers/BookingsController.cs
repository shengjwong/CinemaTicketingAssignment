using System.Globalization;
using System.Security.Claims;
using System.Text;
using CinemaTicketingAssignment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketingAssignment.Controllers;

[Authorize(Roles = "MEMBER")]
public class BookingsController : Controller
{
    private static readonly TimeSpan HoldDuration = TimeSpan.FromMinutes(8);
    private readonly DB _db;

    public BookingsController(DB db) => _db = db;

    // ==========================================
    // 1. Seat Selection Page
    // ==========================================
    public async Task<IActionResult> SelectSeats(string showtimeId)
    {
        var showtime = await _db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.CinemaHall)
            .FirstOrDefaultAsync(s => s.Id == showtimeId);

        if (showtime is null) return NotFound();

        var userId = CurrentUserId();
        await ClearExpiredHoldsAsync();

        var holds = await _db.SeatHolds.Where(h => h.ShowtimeId == showtimeId).ToListAsync();
        var capacity = showtime.CinemaHall.SeatCapacity;
        var columns = capacity <= 80 ? 8 : 10;

        return View(new SeatSelectionViewModel
        {
            Showtime = showtime,
            BookedSeats = await _db.BookingSeats.Where(s => s.ShowtimeId == showtimeId).Select(s => s.SeatCode).ToListAsync(),
            LockedSeats = holds.Where(h => h.UserId != userId).Select(h => h.SeatCode).ToList(),
            MyHeldSeats = holds.Where(h => h.UserId == userId).Select(h => h.SeatCode).ToList(),
            Columns = columns,
            Rows = (int)Math.Ceiling(capacity / (double)columns)
        });
    }

    // ==========================================
    // 2. Lock / Unlock Seats via AJAX
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSeatHolds([FromBody] SeatHoldRequest request)
    {
        if (request.Seats is null || request.Seats.Count > 10)
            return BadRequest(new { message = "Choose up to 10 seats." });

        var showtime = await _db.Showtimes.Include(s => s.CinemaHall).FirstOrDefaultAsync(s => s.Id == request.ShowtimeId);
        if (showtime is null) return NotFound();

        var seats = request.Seats.Select(NormalizeSeat).Where(s => s is not null).Select(s => s!).Distinct().ToList();
        if (seats.Count != request.Seats.Count || !SeatsFitHall(seats, showtime.CinemaHall.SeatCapacity))
            return BadRequest(new { message = "An invalid seat was selected." });

        var userId = CurrentUserId();
        await ClearExpiredHoldsAsync();
        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            var booked = await _db.BookingSeats.Where(s => s.ShowtimeId == request.ShowtimeId && seats.Contains(s.SeatCode)).ToListAsync();
            if (booked.Count > 0)
                return Conflict(new { message = $"{string.Join(", ", booked.Select(s => s.SeatCode))} was just booked by another customer." });

            var myHolds = await _db.SeatHolds.Where(h => h.ShowtimeId == request.ShowtimeId && h.UserId == userId).ToListAsync();
            _db.SeatHolds.RemoveRange(myHolds.Where(h => !seats.Contains(h.SeatCode)));

            var heldCodes = myHolds.Select(h => h.SeatCode).ToHashSet();
            foreach (var seat in seats.Where(s => !heldCodes.Contains(s)))
            {
                _db.SeatHolds.Add(new SeatHold
                {
                    ShowtimeId = request.ShowtimeId,
                    SeatCode = seat,
                    UserId = userId,
                    ExpiresAtUtc = DateTime.UtcNow.Add(HoldDuration)
                });
            }

            foreach (var hold in myHolds.Where(h => seats.Contains(h.SeatCode)))
            {
                hold.ExpiresAtUtc = DateTime.UtcNow.Add(HoldDuration);
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok(new { seats, expiresAtUtc = DateTime.UtcNow.Add(HoldDuration) });
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            _db.ChangeTracker.Clear();
            return Conflict(new { message = "One or more seats are being held by another customer. Please choose another seat." });
        }
    }

    // ==========================================
    // 3. Checkout & Payment Page
    // ==========================================
    public async Task<IActionResult> Payment(string showtimeId)
    {
        var userId = CurrentUserId();
        await ClearExpiredHoldsAsync();

        var showtime = await _db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.CinemaHall)
            .FirstOrDefaultAsync(s => s.Id == showtimeId);

        if (showtime is null) return NotFound();

        var seats = await _db.SeatHolds
            .Where(h => h.ShowtimeId == showtimeId && h.UserId == userId)
            .OrderBy(h => h.SeatCode)
            .Select(h => h.SeatCode)
            .ToListAsync();

        if (seats.Count == 0)
        {
            TempData["Error"] = "Your seat hold expired. Please select seats again.";
            return RedirectToAction(nameof(SelectSeats), new { showtimeId });
        }

        ViewBag.Seats = seats;
        return View(showtime);
    }

    // ==========================================
    // 4. Confirm Payment (Card / Sandbox)
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPayment(string showtimeId, string cardNumber, string cardHolder)
    {
        if (string.IsNullOrWhiteSpace(cardHolder) || cardNumber?.Replace(" ", "").Length < 12)
        {
            TempData["Error"] = "Enter valid sandbox card details (for example 4242 4242 4242 4242).";
            return RedirectToAction(nameof(Payment), new { showtimeId });
        }

        try
        {
            var booking = await CompleteBookingAsync(
                showtimeId,
                "Paid (Stripe Sandbox)",
                "pi_test_" + Guid.NewGuid().ToString("N")[..12]);

            TempData["PaymentSuccess"] = "Payment successful! Your movie tickets have been confirmed.";
            return RedirectToAction(nameof(Receipt), new { id = booking.Id });
        }
        catch (Exception ex) when (ex is DbUpdateException or InvalidOperationException)
        {
            _db.ChangeTracker.Clear();
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(SelectSeats), new { showtimeId });
        }
    }

    // ==========================================
    // 5. Confirm PayPal Payment (Client JS Integration)
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmClientPayPalPayment([FromBody] ClientPayPalPaymentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId))
            return BadRequest(new { message = "A PayPal order ID is required." });

        try
        {
            var booking = await CompleteBookingAsync(
                request.ShowtimeId,
                "Paid (PayPal Client Demo)",
                $"paypal_{request.OrderId}");

            TempData["PaymentSuccess"] = "PayPal Sandbox payment successful! Your movie tickets have been confirmed.";
            return Ok(new { receiptUrl = Url.Action(nameof(Receipt), new { id = booking.Id }) });
        }
        catch (InvalidOperationException ex)
        {
            _db.ChangeTracker.Clear();
            return BadRequest(new { message = ex.Message });
        }
    }

    // ==========================================
    // 6. User Booking History
    // ==========================================
    public async Task<IActionResult> History()
    {
        var userId = CurrentUserId();
        var bookings = await _db.Bookings
            .Where(b => b.UserId == userId)
            .Include(b => b.Showtime).ThenInclude(s => s.Movie)
            .Include(b => b.Showtime.CinemaHall)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

        return View(bookings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(string id)
    {
        var booking = await _db.Bookings
            .Include(b => b.Seats)
            .Include(b => b.Showtime)
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == CurrentUserId());

        if (booking is null) return NotFound();
        if (booking.PaymentStatus.StartsWith("Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "This booking has already been cancelled.";
            return RedirectToAction(nameof(History));
        }
        if (booking.Showtime.StartDateTime <= DateTime.Now)
        {
            TempData["Error"] = "A booking cannot be cancelled after its showtime has started.";
            return RedirectToAction(nameof(History));
        }

        booking.PaymentStatus = "Cancelled";
        booking.PaymentReference = $"Cancelled {DateTime.UtcNow:yyyyMMddHHmm}";
        _db.BookingSeats.RemoveRange(booking.Seats);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Booking cancelled. Your seats are available again.";
        return RedirectToAction(nameof(History));
    }

    // ==========================================
    // 7. Receipt View
    // ==========================================
    public async Task<IActionResult> Receipt(string id)
    {
        var booking = await GetOwnedBooking(id);
        return booking is null ? NotFound() : View(booking);
    }

    // ==========================================
    // 8. Download E-Ticket (PDF)
    // ==========================================
    public async Task<IActionResult> DownloadTicket(string id)
    {
        var booking = await GetOwnedBooking(id);
        if (booking is null) return NotFound();

        return File(BuildPdf(booking), "application/pdf", $"CinemaTicket-{booking.Id}.pdf");
    }

    // ==========================================
    // PRIVATE HELPER METHODS
    // ==========================================
    private async Task<Booking?> GetOwnedBooking(string id) =>
        await _db.Bookings
            .Include(b => b.Showtime).ThenInclude(s => s.Movie)
            .Include(b => b.Showtime.CinemaHall)
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == CurrentUserId());

    private async Task<(Showtime Showtime, List<SeatHold> Seats)> GetHeldShowtimeAsync(string showtimeId)
    {
        await ClearExpiredHoldsAsync();

        var showtime = await _db.Showtimes.FirstOrDefaultAsync(s => s.Id == showtimeId)
            ?? throw new InvalidOperationException("Showtime was not found.");

        var seats = await _db.SeatHolds
            .Where(h => h.ShowtimeId == showtimeId && h.UserId == CurrentUserId())
            .OrderBy(h => h.SeatCode)
            .ToListAsync();

        if (seats.Count == 0)
            throw new InvalidOperationException("Your seat hold expired. Please select seats again.");

        return (showtime, seats);
    }

    private async Task<Booking> CompleteBookingAsync(string showtimeId, string paymentStatus, string paymentReference)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var (showtime, heldSeats) = await GetHeldShowtimeAsync(showtimeId);
            var seatCodes = heldSeats.Select(h => h.SeatCode).ToList();

            if (await _db.BookingSeats.AnyAsync(s => s.ShowtimeId == showtimeId && seatCodes.Contains(s.SeatCode)))
                throw new InvalidOperationException("A selected seat was just booked. Please choose another seat.");

            var booking = new Booking
            {
                Id = "BK" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                UserId = CurrentUserId(),
                ShowtimeId = showtimeId,
                SelectedSeats = string.Join(", ", seatCodes),
                TotalAmount = showtime.TicketPrice * seatCodes.Count,
                PaymentStatus = paymentStatus,
                PaymentReference = paymentReference,
                BookingDate = DateTime.UtcNow
            };

            _db.Bookings.Add(booking);
            _db.BookingSeats.AddRange(seatCodes.Select(code => new BookingSeat
            {
                BookingId = booking.Id,
                ShowtimeId = showtimeId,
                SeatCode = code
            }));

            _db.SeatHolds.RemoveRange(heldSeats);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return booking;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task ClearExpiredHoldsAsync()
    {
        var expired = await _db.SeatHolds.Where(h => h.ExpiresAtUtc <= DateTime.UtcNow).ToListAsync();
        if (expired.Count > 0)
        {
            _db.SeatHolds.RemoveRange(expired);
            await _db.SaveChangesAsync();
        }
    }

    private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();

    private static string? NormalizeSeat(string? raw) => string.IsNullOrWhiteSpace(raw) ? null : raw.Trim().ToUpperInvariant();

    private static bool SeatsFitHall(IEnumerable<string> seats, int capacity)
    {
        var columns = capacity <= 80 ? 8 : 10;
        return seats.All(s => s.Length is >= 2 and <= 4 &&
                              char.IsLetter(s[0]) &&
                              int.TryParse(s[1..], out var n) &&
                              n > 0 &&
                              ((s[0] - 'A') * columns + n) <= capacity);
    }

    private static byte[] BuildPdf(Booking b)
    {
        var lines = new[]
        {
            "CINE-TICKETING - E-TICKET & RECEIPT",
            $"Booking: {b.Id}",
            $"Movie: {b.Showtime.Movie.Title}",
            $"Hall: {b.Showtime.CinemaHall.HallName}",
            $"Showtime: {b.Showtime.StartDateTime:dd MMM yyyy, h:mm tt}",
            $"Seats: {b.SelectedSeats}",
            $"Amount: RM {b.TotalAmount:0.00}",
            $"Payment: {b.PaymentStatus}",
            $"Reference: {b.PaymentReference}",
            "Present this ticket at the cinema entrance."
        };

        var content = new StringBuilder("BT /F1 16 Tf 50 760 Td ");
        for (var i = 0; i < lines.Length; i++) content.Append($"({EscapePdf(lines[i])}) Tj 0 -28 Td ");
        content.Append("ET");

        var body = content.ToString();
        var pdf = $"%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Count 1/Kids[3 0 R]>>endobj\n3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Resources<</Font<</F1 4 0 R>>>>/Contents 5 0 R>>endobj\n4 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj\n5 0 obj<</Length {Encoding.ASCII.GetByteCount(body)}>>stream\n{body}\nendstream\nendobj\ntrailer<</Root 1 0 R>>\n%%EOF";

        return Encoding.ASCII.GetBytes(pdf);
    }

    private static string EscapePdf(string text) => text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}

public class SeatHoldRequest
{
    public string ShowtimeId { get; set; } = string.Empty;
    public List<string> Seats { get; set; } = [];
}

public class ClientPayPalPaymentRequest
{
    public string ShowtimeId { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
}

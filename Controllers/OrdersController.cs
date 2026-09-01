using CinemaTicketingAssignment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketingAssignment.Controllers;

[Authorize(Roles = "ADMIN")]
public class OrdersController : Controller
{
    private const int PageSize = 10;
    private readonly DB _db;

    public OrdersController(DB db) => _db = db;

    public async Task<IActionResult> Index(string? search, string? status, string? sort, int page = 1)
        => View(await BuildListAsync(search, status, sort, page));

    [HttpGet]
    public async Task<IActionResult> Search(string? search, string? status, string? sort, int page = 1)
        => PartialView("_OrderResults", await BuildListAsync(search, status, sort, page));

    public async Task<IActionResult> Details(string type, string id)
    {
        if (string.Equals(type, "Booking", StringComparison.OrdinalIgnoreCase))
        {
            var booking = await _db.Bookings.Include(b => b.User).Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Showtime.CinemaHall).FirstOrDefaultAsync(b => b.Id == id);
            if (booking is null) return NotFound();
            return View(new OrderDetailViewModel
            {
                Order = ToListItem(booking),
                MovieTitle = booking.Showtime.Movie.Title,
                HallName = booking.Showtime.CinemaHall.HallName,
                Showtime = booking.Showtime.StartDateTime,
                Seats = booking.SelectedSeats
            });
        }

        if (string.Equals(type, "Food", StringComparison.OrdinalIgnoreCase))
        {
            var order = await _db.FoodOrders.Include(o => o.User).Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
            if (order is null) return NotFound();
            return View(new OrderDetailViewModel
            {
                Order = ToListItem(order),
                Items = order.Items.Select(i => (i.ItemName, i.Quantity, i.UnitPrice)).ToList()
            });
        }

        return NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel([FromBody] CancelOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Id) || string.IsNullOrWhiteSpace(request.Type))
            return BadRequest(new { message = "Order type and ID are required." });

        if (string.Equals(request.Type, "Booking", StringComparison.OrdinalIgnoreCase))
        {
            var booking = await _db.Bookings.Include(b => b.Seats).FirstOrDefaultAsync(b => b.Id == request.Id);
            if (booking is null) return NotFound(new { message = "Ticket order was not found." });
            if (IsCancelled(booking.PaymentStatus)) return BadRequest(new { message = "This order has already been cancelled." });

            booking.PaymentStatus = "Cancelled";
            booking.PaymentReference = $"Cancelled {DateTime.UtcNow:yyyyMMddHHmm}";
            _db.BookingSeats.RemoveRange(booking.Seats);
        }
        else
        {
            return BadRequest(new { message = "Food orders are immediate and cannot be cancelled." });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Order cancelled. Ticket seats are available again." });
    }

    private async Task<OrderListViewModel> BuildListAsync(string? search, string? status, string? sort, int page)
    {
        var bookings = await _db.Bookings.AsNoTracking().Include(b => b.User).Include(b => b.Showtime).ThenInclude(s => s.Movie).ToListAsync();
        var foodOrders = await _db.FoodOrders.AsNoTracking().Include(o => o.User).Include(o => o.Items).ToListAsync();
        IEnumerable<OrderListItemViewModel> query = bookings.Select(ToListItem).Concat(foodOrders.Select(ToListItem));

        var normalizedSearch = search?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(o => string.Join(' ', o.Id, o.Type, o.CustomerName, o.CustomerEmail, o.Summary)
                .Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase));
        }

        var normalizedStatus = string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase) ? "Cancelled"
            : string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase) ? "Paid" : "All";
        if (normalizedStatus != "All")
            query = query.Where(o => normalizedStatus == "Cancelled" ? IsCancelled(o.Status) : !IsCancelled(o.Status));

        var normalizedSort = sort switch
        {
            "date_asc" or "amount_desc" or "amount_asc" or "customer_asc" => sort,
            _ => "date_desc"
        };
        query = normalizedSort switch
        {
            "date_asc" => query.OrderBy(o => o.OrderedAt),
            "amount_desc" => query.OrderByDescending(o => o.TotalAmount),
            "amount_asc" => query.OrderBy(o => o.TotalAmount),
            "customer_asc" => query.OrderBy(o => o.CustomerName),
            _ => query.OrderByDescending(o => o.OrderedAt)
        };

        var all = query.ToList();
        var totalPages = Math.Max(1, (int)Math.Ceiling(all.Count / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);
        return new OrderListViewModel
        {
            Orders = all.Skip((page - 1) * PageSize).Take(PageSize).ToList(),
            Search = normalizedSearch ?? string.Empty,
            Status = normalizedStatus,
            Sort = normalizedSort,
            Page = page,
            TotalPages = totalPages,
            TotalCount = all.Count
        };
    }

    private static bool IsCancelled(string status) => status.StartsWith("Cancelled", StringComparison.OrdinalIgnoreCase);

    private static OrderListItemViewModel ToListItem(Booking booking) => new()
    {
        Id = booking.Id, Type = "Booking", CustomerName = booking.User.Name, CustomerEmail = booking.User.Email,
        Summary = booking.Showtime.Movie.Title, TotalAmount = booking.TotalAmount, Status = booking.PaymentStatus, OrderedAt = booking.BookingDate
    };

    private static OrderListItemViewModel ToListItem(FoodOrder order) => new()
    {
        Id = order.Id, Type = "Food", CustomerName = order.User.Name, CustomerEmail = order.User.Email,
        Summary = string.Join(", ", order.Items.Select(i => $"{i.ItemName} x{i.Quantity}")), TotalAmount = order.TotalAmount, Status = order.PaymentStatus, OrderedAt = order.OrderDate
    };

    public sealed class CancelOrderRequest { public string Type { get; set; } = string.Empty; public string Id { get; set; } = string.Empty; }
}

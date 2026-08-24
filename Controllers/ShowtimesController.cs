using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaTicketingAssignment.Models;

namespace CinemaTicketingAssignment.Controllers;

public class ShowtimesController : Controller
{
    private readonly DB _db;

    public ShowtimesController(DB db)
    {
        _db = db;
    }

    // 1. Admin 专属排片管理列表
    public async Task<IActionResult> Index()
    {
        if (!User.IsInRole("ADMIN")) return Forbid();

        var showtimes = await _db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.CinemaHall)
            .OrderByDescending(s => s.StartDateTime)
            .ToListAsync();

        return View(showtimes);
    }

    // 2. 普通用户场次选择页面 (支持按 MovieId 过滤)
    public async Task<IActionResult> CustomerIndex(string movieId)
    {
        var currentTime = DateTime.Now;
        var query = _db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.CinemaHall)
            .AsQueryable();

        if (!string.IsNullOrEmpty(movieId))
        {
            query = query.Where(s => s.MovieId == movieId);
            ViewBag.SelectedMovie = await _db.Movies.FindAsync(movieId);
        }

        var showtimes = await query
            .Where(s => s.EndDateTime > currentTime)
            .OrderBy(s => s.StartDateTime)
            .ToListAsync();

        return View(showtimes);
    }

    // AJAX availability check for the selected hall/time.
    [HttpGet]
    public async Task<IActionResult> CheckAvailability(string hallId, DateTime startDateTime, DateTime endDateTime, string? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(hallId) || endDateTime <= startDateTime)
            return Json(new { available = false, message = "Please provide a valid hall and time range." });

        var conflict = await _db.Showtimes.AnyAsync(s =>
            s.Id != excludeId &&
            s.CinemaHallId == hallId &&
            startDateTime < s.EndDateTime &&
            endDateTime > s.StartDateTime);

        return Json(new
        {
            available = !conflict,
            message = conflict ? "Hall is unavailable: another showtime overlaps this period." : "Hall is available for this period."
        });
    }

    // 3. 查看场次详情
    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var showtime = await _db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.CinemaHall)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (showtime == null) return NotFound();

        return View(showtime);
    }

    // 4. 新建场次 (GET)
    public async Task<IActionResult> Create()
    {
        if (!User.IsInRole("ADMIN")) return Forbid();

        ViewBag.Movies = await _db.Movies.ToListAsync();
        ViewBag.CinemaHalls = await _db.CinemaHalls.ToListAsync();
        return View();
    }

    // 4. 新建场次 (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Showtime showtime)
    {
        if (!User.IsInRole("ADMIN")) return Forbid();

        ModelState.Remove("Movie");
        ModelState.Remove("CinemaHall");

        ValidateShowtime(showtime);
        if (await HasHallConflictAsync(showtime))
            ModelState.AddModelError(string.Empty, "This cinema hall already has another showtime overlapping the selected time.");

        if (ModelState.IsValid)
        {
            showtime.Id = "SHOW" + Guid.NewGuid().ToString("N")[..5].ToUpper();
            _db.Add(showtime);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Showtime created successfully!";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Movies = await _db.Movies.ToListAsync();
        ViewBag.CinemaHalls = await _db.CinemaHalls.ToListAsync();
        return View(showtime);
    }

    private void ValidateShowtime(Showtime showtime)
    {
        if (string.IsNullOrWhiteSpace(showtime.MovieId)) ModelState.AddModelError(nameof(showtime.MovieId), "Please select a movie.");
        if (string.IsNullOrWhiteSpace(showtime.CinemaHallId)) ModelState.AddModelError(nameof(showtime.CinemaHallId), "Please select a cinema hall.");
        if (showtime.EndDateTime <= showtime.StartDateTime) ModelState.AddModelError(nameof(showtime.EndDateTime), "End time must be later than start time.");
        if (showtime.StartDateTime < DateTime.Now) ModelState.AddModelError(nameof(showtime.StartDateTime), "Showtime cannot start in the past.");
        if (showtime.TicketPrice < 0) ModelState.AddModelError(nameof(showtime.TicketPrice), "Ticket price cannot be negative.");
    }

    private async Task<bool> HasHallConflictAsync(Showtime showtime)
    {
        if (string.IsNullOrWhiteSpace(showtime.CinemaHallId) || showtime.EndDateTime <= showtime.StartDateTime) return false;

        return await _db.Showtimes.AnyAsync(s =>
            s.Id != showtime.Id &&
            s.CinemaHallId == showtime.CinemaHallId &&
            showtime.StartDateTime < s.EndDateTime &&
            showtime.EndDateTime > s.StartDateTime);
    }

    // 5. 编辑场次 (GET)
    public async Task<IActionResult> Edit(string id)
    {
        if (!User.IsInRole("ADMIN")) return Forbid();
        if (string.IsNullOrEmpty(id)) return NotFound();

        var showtime = await _db.Showtimes.FindAsync(id);
        if (showtime == null) return NotFound();

        ViewBag.Movies = await _db.Movies.ToListAsync();
        ViewBag.CinemaHalls = await _db.CinemaHalls.ToListAsync();
        return View(showtime);
    }

    // 5. 编辑场次 (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, Showtime showtime)
    {
        if (!User.IsInRole("ADMIN")) return Forbid();
        if (id != showtime.Id) return NotFound();

        ModelState.Remove("Movie");
        ModelState.Remove("CinemaHall");

        ValidateShowtime(showtime);
        if (await HasHallConflictAsync(showtime))
            ModelState.AddModelError(string.Empty, "This cinema hall already has another showtime overlapping the selected time.");

        if (ModelState.IsValid)
        {
            try
            {
                _db.Update(showtime);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Showtime updated successfully!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_db.Showtimes.Any(e => e.Id == showtime.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Movies = await _db.Movies.ToListAsync();
        ViewBag.CinemaHalls = await _db.CinemaHalls.ToListAsync();
        return View(showtime);
    }

    // 6. 删除场次 (POST)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (!User.IsInRole("ADMIN")) return Forbid();

        var showtime = await _db.Showtimes.FindAsync(id);
        if (showtime != null)
        {
            _db.Showtimes.Remove(showtime);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Showtime deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
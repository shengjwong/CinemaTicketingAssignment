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

        if (string.IsNullOrEmpty(showtime.Id))
        {
            showtime.Id = "SHOW" + Guid.NewGuid().ToString("N")[..5].ToUpper();
        }

        ModelState.Remove("Movie");
        ModelState.Remove("CinemaHall");

        if (ModelState.IsValid)
        {
            _db.Add(showtime);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Showtime created successfully!";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Movies = await _db.Movies.ToListAsync();
        ViewBag.CinemaHalls = await _db.CinemaHalls.ToListAsync();
        return View(showtime);
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
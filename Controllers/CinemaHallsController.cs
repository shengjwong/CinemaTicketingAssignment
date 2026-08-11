using CinemaTicketingAssignment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketingAssignment.Controllers;

[Authorize(Roles = "ADMIN")]
public class CinemaHallsController : Controller
{
    private readonly DB db;

    public CinemaHallsController(DB db)
    {
        this.db = db;
    }

    // GET: /CinemaHalls
    public async Task<IActionResult> Index()
    {
        var halls = await db.CinemaHalls.OrderBy(h => h.HallName).ToListAsync();
        return View(halls);
    }

    // GET: /CinemaHalls/Details/HALL001
    public async Task<IActionResult> Details(string id)
    {
        var hall = await db.CinemaHalls
            .Include(h => h.Showtimes)
            .ThenInclude(s => s.Movie)
            .FirstOrDefaultAsync(h => h.Id == id);

        if (hall == null) return NotFound();
        return View(hall);
    }

    // GET: /CinemaHalls/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: /CinemaHalls/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CinemaHall hall)
    {
        if (!ModelState.IsValid) return View(hall);

        hall.Id = await GenerateNextIdAsync();
        db.CinemaHalls.Add(hall);
        await db.SaveChangesAsync();

        TempData["Success"] = $"Cinema hall '{hall.HallName}' created.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /CinemaHalls/Edit/HALL001
    public async Task<IActionResult> Edit(string id)
    {
        var hall = await db.CinemaHalls.FindAsync(id);
        if (hall == null) return NotFound();
        return View(hall);
    }

    // POST: /CinemaHalls/Edit/HALL001
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, CinemaHall hall)
    {
        if (id != hall.Id) return NotFound();
        if (!ModelState.IsValid) return View(hall);

        db.CinemaHalls.Update(hall);
        await db.SaveChangesAsync();

        TempData["Success"] = $"Cinema hall '{hall.HallName}' updated.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /CinemaHalls/Delete/HALL001
    public async Task<IActionResult> Delete(string id)
    {
        var hall = await db.CinemaHalls.FirstOrDefaultAsync(h => h.Id == id);
        if (hall == null) return NotFound();
        return View(hall);
    }

    // POST: /CinemaHalls/Delete/HALL001
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var hall = await db.CinemaHalls.FindAsync(id);
        if (hall == null) return NotFound();

        var hasShowtimes = await db.Showtimes.AnyAsync(s => s.CinemaHallId == id);
        if (hasShowtimes)
        {
            TempData["Error"] = "Cannot delete a hall that still has showtimes scheduled in it.";
            return RedirectToAction(nameof(Index));
        }

        db.CinemaHalls.Remove(hall);
        await db.SaveChangesAsync();

        TempData["Success"] = "Cinema hall deleted.";
        return RedirectToAction(nameof(Index));
    }

    // Generates the next HALLnnn id, matching the seed data's ID scheme.
    private async Task<string> GenerateNextIdAsync()
    {
        var lastId = await db.CinemaHalls
            .OrderByDescending(h => h.Id)
            .Select(h => h.Id)
            .FirstOrDefaultAsync();

        var nextNumber = 1;
        if (!string.IsNullOrEmpty(lastId) && int.TryParse(lastId.Replace("HALL", ""), out var n))
            nextNumber = n + 1;

        return $"HALL{nextNumber:000}";
    }
}

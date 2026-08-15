using CinemaTicketingAssignment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketingAssignment.Controllers;

[Authorize(Roles = "ADMIN")]
public class ShowtimesController : Controller
{
    private readonly DB db;

    public ShowtimesController(DB db)
    {
        this.db = db;
    }

    // GET: /Showtimes
    public async Task<IActionResult> Index()
    {
        var showtimes = await db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.CinemaHall)
            .OrderBy(s => s.StartDateTime)
            .ToListAsync();

        return View(showtimes);
    }

    // GET: /Showtimes/Details/SHOW001
    public async Task<IActionResult> Details(string id)
    {
        var showtime = await db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.CinemaHall)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (showtime == null) return NotFound();
        return View(showtime);
    }

    // GET: /Showtimes/Create
    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        return View();
    }

    // POST: /Showtimes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Showtime showtime)
    {
        // 1. Clear Id validation so empty client values don't block ModelState
        ModelState.Remove(nameof(showtime.Id));

        await ValidateNoHallOverlapAsync(showtime);

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(showtime);
        }

        // 2. Assign server-generated ID
        showtime.Id = await GenerateNextIdAsync();
        db.Showtimes.Add(showtime);
        await db.SaveChangesAsync();

        TempData["Success"] = "Showtime scheduled.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Showtimes/Edit/SHOW001
    public async Task<IActionResult> Edit(string id)
    {
        var showtime = await db.Showtimes.FindAsync(id);
        if (showtime == null) return NotFound();

        await PopulateDropdownsAsync();
        return View(showtime);
    }

    // POST: /Showtimes/Edit/SHOW001
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, Showtime showtime)
    {
        if (id != showtime.Id) return NotFound();

        await ValidateNoHallOverlapAsync(showtime, excludingId: id);

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(showtime);
        }

        db.Showtimes.Update(showtime);
        await db.SaveChangesAsync();

        TempData["Success"] = "Showtime updated.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Showtimes/Delete/SHOW001
    public async Task<IActionResult> Delete(string id)
    {
        var showtime = await db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.CinemaHall)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (showtime == null) return NotFound();
        return View(showtime);
    }

    // POST: /Showtimes/Delete/SHOW001
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var showtime = await db.Showtimes.FindAsync(id);
        if (showtime == null) return NotFound();

        // Check for existing bookings tied to this showtime
        var hasBookings = await db.Bookings.AnyAsync(b => b.ShowtimeId == id);
        if (hasBookings)
        {
            TempData["Error"] = "Cannot delete a showtime that already has customer bookings.";
            return RedirectToAction(nameof(Index));
        }

        db.Showtimes.Remove(showtime);
        await db.SaveChangesAsync();

        TempData["Success"] = "Showtime cancelled.";
        return RedirectToAction(nameof(Index));
    }

    // Prevents scheduling two showtimes in the same hall with overlapping time ranges.
    private async Task ValidateNoHallOverlapAsync(Showtime showtime, string? excludingId = null)
    {
        if (showtime.EndDateTime <= showtime.StartDateTime)
        {
            ModelState.AddModelError(nameof(Showtime.EndDateTime), "End time must be after the start time.");
            return;
        }

        var overlapExists = await db.Showtimes
            .Where(s => s.CinemaHallId == showtime.CinemaHallId && s.Id != excludingId)
            .AnyAsync(s => showtime.StartDateTime < s.EndDateTime && s.StartDateTime < showtime.EndDateTime);

        if (overlapExists)
        {
            ModelState.AddModelError(string.Empty,
                "This hall already has a showtime scheduled that overlaps with the selected time range.");
        }
    }

    private async Task PopulateDropdownsAsync()
    {
        ViewBag.Movies = await db.Movies.OrderBy(m => m.Title).ToListAsync();
        ViewBag.CinemaHalls = await db.CinemaHalls.OrderBy(h => h.HallName).ToListAsync();
    }

    // Safely parses existing SHOWnnn IDs in memory to find the highest sequence number
    private async Task<string> GenerateNextIdAsync()
    {
        var allIds = await db.Showtimes.Select(s => s.Id).ToListAsync();

        int maxNumber = 0;
        foreach (var id in allIds)
        {
            if (id.StartsWith("SHOW") && int.TryParse(id.Substring(4), out var num))
            {
                if (num > maxNumber) maxNumber = num;
            }
        }

        return $"SHOW{(maxNumber + 1):000}";
    }
}
using CinemaTicketingAssignment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketingAssignment.Controllers;

public class MoviesController : Controller
{
    private readonly DB db;

    public MoviesController(DB db)
    {
        this.db = db;
    }

    // GET: /Movies
    // Public browsing page: Now Showing / Coming Soon tabs + Genre/Language/Age filters.
    public async Task<IActionResult> Index(string? genre, string? language, string? age, MovieStatus status = MovieStatus.NowShowing)
    {
        var query = db.Movies.AsQueryable();

        query = query.Where(m => m.Status == status);

        if (!string.IsNullOrEmpty(genre))
            query = query.Where(m => m.Genre == genre);

        if (!string.IsNullOrEmpty(language))
            query = query.Where(m => m.Language == language);

        if (!string.IsNullOrEmpty(age))
            query = query.Where(m => m.AgeClassification == age);

        ViewBag.Genres = await db.Movies.Select(m => m.Genre).Distinct().OrderBy(g => g).ToListAsync();
        ViewBag.Languages = await db.Movies.Select(m => m.Language).Distinct().OrderBy(l => l).ToListAsync();
        ViewBag.AgeClassifications = await db.Movies.Select(m => m.AgeClassification).Distinct().OrderBy(a => a).ToListAsync();

        ViewBag.SelectedGenre = genre;
        ViewBag.SelectedLanguage = language;
        ViewBag.SelectedAge = age;
        ViewBag.SelectedStatus = status;

        var movies = await query.OrderBy(m => m.Title).ToListAsync();
        return View(movies);
    }

    // GET: /Movies/Details/MOV001
    public async Task<IActionResult> Details(string id)
    {
        var movie = await db.Movies
            .Include(m => m.Showtimes)
            .ThenInclude(s => s.CinemaHall)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (movie == null) return NotFound();

        ViewBag.Reviews = await db.Reviews
            .Where(r => r.MovieId == movie.Id)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(movie);
    }

    // ---------------- Admin CRUD ----------------

    // GET: /Movies/Admin
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Admin()
    {
        var movies = await db.Movies.OrderBy(m => m.Title).ToListAsync();
        return View(movies);
    }

    // GET: /Movies/Create
    [Authorize(Roles = "ADMIN")]
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Movies/Create
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Movie movie)
    {
        // 1. Remove Id from validation so empty input doesn't fail ModelState
        ModelState.Remove(nameof(movie.Id));

        if (!ModelState.IsValid) return View(movie);

        // 2. Generate and assign next MOVnnn key
        movie.Id = await GenerateNextIdAsync();

        db.Movies.Add(movie);
        await db.SaveChangesAsync();

        TempData["Success"] = $"Movie '{movie.Title}' created.";
        return RedirectToAction(nameof(Admin));
    }

    // GET: /Movies/Edit/MOV001
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Edit(string id)
    {
        var movie = await db.Movies.FindAsync(id);
        if (movie == null) return NotFound();
        return View(movie);
    }

    // POST: /Movies/Edit/MOV001
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, Movie movie)
    {
        if (id != movie.Id) return NotFound();
        if (!ModelState.IsValid) return View(movie);

        db.Movies.Update(movie);
        await db.SaveChangesAsync();

        TempData["Success"] = $"Movie '{movie.Title}' updated.";
        return RedirectToAction(nameof(Admin));
    }

    // GET: /Movies/Delete/MOV001
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Delete(string id)
    {
        var movie = await db.Movies.FirstOrDefaultAsync(m => m.Id == id);
        if (movie == null) return NotFound();
        return View(movie);
    }

    // POST: /Movies/Delete/MOV001
    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "ADMIN")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var movie = await db.Movies.FindAsync(id);
        if (movie == null) return NotFound();

        // Safety check to prevent deleting movies tied to showtimes
        var hasShowtimes = await db.Showtimes.AnyAsync(s => s.MovieId == id);
        if (hasShowtimes)
        {
            TempData["Error"] = "Cannot delete a movie that is currently linked to existing showtimes.";
            return RedirectToAction(nameof(Admin));
        }

        db.Movies.Remove(movie);
        await db.SaveChangesAsync();

        TempData["Success"] = "Movie deleted.";
        return RedirectToAction(nameof(Admin));
    }

    // Safely parses existing IDs in-memory to find the highest numerical sequence
    private async Task<string> GenerateNextIdAsync()
    {
        var allIds = await db.Movies.Select(m => m.Id).ToListAsync();

        int maxNumber = 0;
        foreach (var id in allIds)
        {
            if (id.StartsWith("MOV") && int.TryParse(id.Substring(3), out var num))
            {
                if (num > maxNumber) maxNumber = num;
            }
        }

        return $"MOV{(maxNumber + 1):000}";
    }
}
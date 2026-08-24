using CinemaTicketingAssignment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.Text.RegularExpressions;

namespace CinemaTicketingAssignment.Controllers;

public class MoviesController : Controller
{
    private readonly DB db;
    private readonly IWebHostEnvironment environment;

    public MoviesController(DB db, IWebHostEnvironment environment)
    {
        this.db = db;
        this.environment = environment;
    }

    // GET: /Movies
    // Public browsing page: Now Showing / Coming Soon + filters + optional AJAX search.
    public async Task<IActionResult> Index(string? search, string? genre, string? language, string? age, MovieStatus status = MovieStatus.NowShowing, int page = 1)
    {
        const int pageSize = 8;
        page = Math.Max(1, page);

        var query = BuildMovieQuery(search, genre, language, age, status);
        var totalCount = await query.CountAsync();
        var movies = await query.OrderBy(m => m.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        await PopulateMovieFilterOptionsAsync();

        ViewBag.Search = search;
        ViewBag.SelectedGenre = genre;
        ViewBag.SelectedLanguage = language;
        ViewBag.SelectedAge = age;
        ViewBag.SelectedStatus = status;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        ViewBag.TotalCount = totalCount;

        return View(movies);
    }

    // AJAX catalog endpoint: updates movie cards + pagination without a full page reload.
    [HttpGet]
    public async Task<IActionResult> Search(string? search, string? genre, string? language, string? age, MovieStatus status = MovieStatus.NowShowing, int page = 1)
    {
        const int pageSize = 8;
        page = Math.Max(1, page);

        var query = BuildMovieQuery(search, genre, language, age, status);
        var totalCount = await query.CountAsync();
        var movies = await query.OrderBy(m => m.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        ViewBag.Search = search;
        ViewBag.SelectedGenre = genre;
        ViewBag.SelectedLanguage = language;
        ViewBag.SelectedAge = age;
        ViewBag.SelectedStatus = status;
        ViewBag.TotalCount = totalCount;

        return PartialView("_MovieCatalogResults", movies);
    }

    private IQueryable<Movie> BuildMovieQuery(string? search, string? genre, string? language, string? age, MovieStatus status)
    {
        var query = db.Movies.AsQueryable().Where(m => m.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m => m.Title.Contains(search) || m.Genre.Contains(search) || m.Language.Contains(search));

        if (!string.IsNullOrEmpty(genre)) query = query.Where(m => m.Genre == genre);
        if (!string.IsNullOrEmpty(language)) query = query.Where(m => m.Language == language);
        if (!string.IsNullOrEmpty(age)) query = query.Where(m => m.AgeClassification == age);

        return query;
    }

    private async Task PopulateMovieFilterOptionsAsync()
    {
        ViewBag.Genres = await db.Movies.Select(m => m.Genre).Distinct().OrderBy(g => g).ToListAsync();
        ViewBag.Languages = await db.Movies.Select(m => m.Language).Distinct().OrderBy(l => l).ToListAsync();
        ViewBag.AgeClassifications = await db.Movies.Select(m => m.AgeClassification).Distinct().OrderBy(a => a).ToListAsync();
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
    public async Task<IActionResult> Create(Movie movie, string? croppedPosterData, IFormFile? posterFile)
    {
        // 1. Remove Id from validation so empty input doesn't fail ModelState
        ModelState.Remove(nameof(movie.Id));

        if (!ModelState.IsValid) return View(movie);

        // 2. Generate and assign next MOVnnn key
        movie.Id = await GenerateNextIdAsync();

        if (!string.IsNullOrWhiteSpace(croppedPosterData))
            movie.PosterImagePath = await SaveCroppedPosterAsync(croppedPosterData, movie.Id);
        else if (posterFile is not null && posterFile.Length > 0)
            movie.PosterImagePath = await SaveUploadedPosterAsync(posterFile, movie.Id);

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
    public async Task<IActionResult> Edit(string id, Movie movie, string? croppedPosterData, IFormFile? posterFile)
    {
        if (id != movie.Id) return NotFound();

        var existingMovie = await db.Movies.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        if (existingMovie == null) return NotFound();

        if (!ModelState.IsValid)
        {
            movie.PosterImagePath = existingMovie.PosterImagePath;
            return View(movie);
        }

        if (!string.IsNullOrWhiteSpace(croppedPosterData))
            movie.PosterImagePath = await SaveCroppedPosterAsync(croppedPosterData, movie.Id);
        else if (posterFile is not null && posterFile.Length > 0)
            movie.PosterImagePath = await SaveUploadedPosterAsync(posterFile, movie.Id);
        else
            movie.PosterImagePath = existingMovie.PosterImagePath;

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

    private async Task<string> SaveUploadedPosterAsync(IFormFile file, string movieId)
    {
        if (file.Length <= 0)
            throw new InvalidOperationException("Poster image is empty.");

        if (file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("Poster image must be 5 MB or smaller.");

        var extension = Path.GetExtension(file.FileName);
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only JPG, JPEG, PNG, and WEBP poster images are allowed.");

        var uploads = Path.Combine(environment.WebRootPath, "uploads", "posters");
        Directory.CreateDirectory(uploads);

        var fileName = $"{movieId}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var filePath = Path.Combine(uploads, fileName);

        await using var stream = new FileStream(filePath, FileMode.CreateNew);
        await file.CopyToAsync(stream);

        return $"/uploads/posters/{fileName}";
    }

    private async Task<string> SaveCroppedPosterAsync(string dataUrl, string movieId)
    {
        var match = Regex.Match(dataUrl, @"^data:image/(png|jpeg|jpg|webp);base64,(.+)$", RegexOptions.IgnoreCase);
        if (!match.Success) throw new InvalidOperationException("Invalid poster image data.");

        var extension = match.Groups[1].Value.ToLowerInvariant() switch
        {
            "jpeg" or "jpg" => ".jpg",
            "webp" => ".webp",
            _ => ".png"
        };

        byte[] bytes;
        try { bytes = Convert.FromBase64String(match.Groups[2].Value); }
        catch (FormatException) { throw new InvalidOperationException("Invalid poster image data."); }

        if (bytes.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("Poster image must be 5 MB or smaller.");

        var uploads = Path.Combine(environment.WebRootPath, "uploads", "posters");
        Directory.CreateDirectory(uploads);
        var fileName = $"{movieId}_{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploads, fileName);
        await System.IO.File.WriteAllBytesAsync(filePath, bytes);
        return $"/uploads/posters/{fileName}";
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
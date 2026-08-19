using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaTicketingAssignment.Models;
using System.Security.Claims;

namespace CinemaTicketingAssignment.Controllers;

public class ReviewsController : Controller
{
    private readonly DB _db;

    public ReviewsController(DB db)
    {
        _db = db;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReview(string movieId, int rating, string comment)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var review = new Review
        {
            Id = "REV" + Guid.NewGuid().ToString("N")[..7].ToUpper(),
            MovieId = movieId,
            UserId = userId,
            Rating = rating,
            Comment = comment ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Your review has been published!";
        return RedirectToAction("Details", "Movies", new { id = movieId });
    }

    // Admin 专属：删除违规评论
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReview(string id, string movieId)
    {
        if (!User.IsInRole("ADMIN")) return Forbid();

        var review = await _db.Reviews.FindAsync(id);
        if (review != null)
        {
            _db.Reviews.Remove(review);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Review removed by administrator.";
        }

        return RedirectToAction("Details", "Movies", new { id = movieId });
    }
}
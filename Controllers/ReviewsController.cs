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

        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (rating is < 1 or > 5 || string.IsNullOrWhiteSpace(comment))
        {
            if (isAjax) return BadRequest(new { message = "Please select a star rating and write a comment." });
            TempData["Error"] = "Please select a star rating and write a comment.";
            return RedirectToAction("Details", "Movies", new { id = movieId });
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

        if (isAjax)
        {
            var newAverage = await _db.Reviews.Where(r => r.MovieId == movieId).AverageAsync(r => r.Rating);

            return Ok(new
            {
                id = review.Id,
                rating = review.Rating,
                comment = review.Comment,
                createdAt = review.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                canDelete = User.IsInRole("ADMIN"),
                averageRating = Math.Round(newAverage, 1)
            });
        }

        TempData["Success"] = "Your review has been published!";
        return RedirectToAction("Details", "Movies", new { id = movieId });
    }

    // Admin 专属：删除违规评论
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReview(string id, string movieId)
    {
        if (!User.IsInRole("ADMIN")) return Forbid();

        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        var review = await _db.Reviews.FindAsync(id);
        if (review != null)
        {
            _db.Reviews.Remove(review);
            await _db.SaveChangesAsync();

            if (isAjax)
            {
                var remaining = await _db.Reviews.Where(r => r.MovieId == movieId).ToListAsync();
                var newAverage = remaining.Any() ? remaining.Average(r => r.Rating) : 0;
                return Ok(new { id = review.Id, averageRating = Math.Round(newAverage, 1), reviewCount = remaining.Count });
            }

            TempData["Success"] = "Review removed by administrator.";
        }
        else if (isAjax)
        {
            return NotFound();
        }

        return RedirectToAction("Details", "Movies", new { id = movieId });
    }
}
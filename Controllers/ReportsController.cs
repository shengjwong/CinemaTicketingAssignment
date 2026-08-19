using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaTicketingAssignment.Models;

namespace CinemaTicketingAssignment.Controllers;

public class ReportsController : Controller
{
    private readonly DB _db;

    public ReportsController(DB db)
    {
        _db = db;
    }

    // GET: Reports/Dashboard
    public IActionResult Dashboard()
    {
        return View();
    }

    // AJAX Endpoint for Chart.js Revenue Data
    [HttpGet]
    public async Task<IActionResult> GetRevenueData()
    {
        // 从 Bookings 表按电影统计总营收
        var revenueData = await _db.Bookings
            .Include(b => b.Showtime)
            .ThenInclude(s => s.Movie)
            .GroupBy(b => b.Showtime.Movie.Title)
            .Select(g => new
            {
                MovieTitle = g.Key,
                TotalRevenue = g.Sum(b => b.TotalAmount)
            })
            .ToListAsync();

        return Json(revenueData);
    }
}
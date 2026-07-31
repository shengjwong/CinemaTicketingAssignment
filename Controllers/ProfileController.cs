using CinemaTicketingAssignment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Authorize(Roles = "CUSTOMER")]
public class ProfileController : Controller
{
    private readonly DB _db;

    public ProfileController(DB db)
    {
        _db = db;
    }

    // READ: View own profile
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Securely pull the ID straight from the login cookie
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _db.Users.FindAsync(userId);

        return View(user);
    }

    // UPDATE: Save new details
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(User updatedModel)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (updatedModel.Id != userId) return Forbid(); // Security check!

        // Update name, personal details, avatar here...

        return RedirectToAction("Index");
    }
}
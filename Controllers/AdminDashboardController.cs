using CinemaTicketingAssignment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketingAssignment.Controllers
{
    [Authorize(Roles = "ADMIN")]
    public class AdminDashboardController : Controller
    {
        private readonly DB _db;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AdminDashboardController(DB db, IPasswordHasher<User> passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        // 1. User/Member Listing Page
        public async Task<IActionResult> ManageMember()
        {
            var users = await _db.Users.ToListAsync();
            return View(users);
        }

        // 2. Edit User Details (GET)
        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // 2. Edit User Details (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(User model)
        {
            var user = await _db.Users.FindAsync(model.Id);
            if (user == null) return NotFound();

            user.Name = model.Name;
            user.Email = model.Email;
            user.Phone = model.Phone;

            await _db.SaveChangesAsync();
            TempData["Success"] = "User details updated successfully.";
            return RedirectToAction(nameof(ManageMember));
        }

        // 3. Block / Unblock Account
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlock(string id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                // Unblock user
                user.LockoutEnd = null;
                user.FailedLoginAttempts = 0;
                TempData["Success"] = $"User {user.Name} has been unblocked.";
            }
            else
            {
                // Block user for 100 years
                user.LockoutEnd = DateTime.UtcNow.AddYears(100);
                TempData["Success"] = $"User {user.Name} has been blocked.";
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(ManageMember));
        }

        // 4. Reset User Password
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, string newPassword)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["Error"] = "Password cannot be empty.";
                return RedirectToAction(nameof(ManageMember));
            }

            user.Hash = _passwordHasher.HashPassword(user, newPassword);
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Password for {user.Name} has been reset successfully.";
            return RedirectToAction(nameof(ManageMember));
        }

        // 5. Delete User Account Safely
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(ManageMember));
            }

            try
            {
                _db.Users.Remove(user);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"User {user.Name} has been permanently deleted.";
            }
            catch (DbUpdateException)
            {
                // Catch foreign key violations (e.g., user has existing bookings or cart records)
                TempData["Error"] = $"Cannot delete {user.Name} because they have existing transaction records (Bookings/Reviews). Please Block them instead.";
            }

            return RedirectToAction(nameof(ManageMember));
        }
    }
}
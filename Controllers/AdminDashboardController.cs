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

        // 2. Create User / Admin (Via Modal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(string name, string email, string phone, string password, string role)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Name, Email, and Password are required.";
                return RedirectToAction(nameof(ManageMember));
            }

            if (await _db.Users.AnyAsync(u => u.Email == email))
            {
                TempData["Error"] = "Email address is already registered.";
                return RedirectToAction(nameof(ManageMember));
            }

            string prefix = role == "Admin" ? "ADM" : "MBR";
            string newId = prefix + Guid.NewGuid().ToString("N")[..4].ToUpper();

            User newUser = role == "Admin"
                ? new Admin { Id = newId, Name = name, Email = email, Phone = phone }
                : new Member { Id = newId, Name = name, Email = email, Phone = phone };

            newUser.Hash = _passwordHasher.HashPassword(newUser, password);

            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"New {role} account ({name}) created successfully!";
            return RedirectToAction(nameof(ManageMember));
        }

        // 3. Edit User Details (Via Modal POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, string name, string email, string phone)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(ManageMember));
            }

            // 检查邮箱重复（排除自己）
            if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != id))
            {
                TempData["Error"] = "Email address is already used by another user.";
                return RedirectToAction(nameof(ManageMember));
            }

            user.Name = name;
            user.Email = email;
            user.Phone = phone;

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Details for {user.Name} updated successfully.";
            return RedirectToAction(nameof(ManageMember));
        }

        // 4. Block / Unblock Account
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlock(string id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                user.LockoutEnd = null;
                user.FailedLoginAttempts = 0;
                TempData["Success"] = $"User {user.Name} has been unblocked.";
            }
            else
            {
                user.LockoutEnd = DateTime.UtcNow.AddYears(100);
                TempData["Success"] = $"User {user.Name} has been blocked.";
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(ManageMember));
        }

        // 5. Reset User Password
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

        // 6. Delete User Account Safely
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
                TempData["Error"] = $"Cannot delete {user.Name} because they have existing transaction records (Bookings/Reviews). Please Block them instead.";
            }

            return RedirectToAction(nameof(ManageMember));
        }
    }
}
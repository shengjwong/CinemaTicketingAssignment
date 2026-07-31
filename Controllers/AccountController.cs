using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CinemaTicketingAssignment.Models;

namespace CinemaTicketingAssignment.Controllers
{
    public class AccountController : Controller
    {
        private readonly DB _db;

        public AccountController(DB db)
        {
            _db = db;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1. Fetch the user from the database
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            // 2. Check if user account is currently locked out
            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                var remainingMinutes = Math.Ceiling((user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes);
                ModelState.AddModelError(string.Empty, $"Account locked. Try again in {remainingMinutes} minutes.");
                return View(model);
            }

            // 3. Verify Password (using your 'Hash' property name)
            if (user.Hash == model.Password)
            {
                // Reset failed attempts upon successful login
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                await _db.SaveChangesAsync();

                // 4. Determine the Role dynamically based on the object class type
                string roleName = "CUSTOMER"; // Default fallback
                if (user is Admin)
                {
                    roleName = "ADMIN";
                }
                else if (user is Member)
                {
                    roleName = "CUSTOMER";
                }

                // 5. Set up the manual cookie claims infrastructure
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id), // user.Id is a string now!
                    new Claim(ClaimTypes.Name, user.Name),         // Changed to user.Name
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, roleName)          // RBAC dynamically set
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                };

                // Issue the authentication cookie
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                // Redirect based on role requirements
                if (roleName == "ADMIN")
                {
                    return RedirectToAction("Index", "AdminDashboard"); // Change to your actual Admin Controller
                }
                return RedirectToAction("Index", "Home");
            }
            else
            {
                // 6. Failed password logic: Handle the 15-minute temporary lockout rules
                user.FailedLoginAttempts++;

                if (user.FailedLoginAttempts >= 3)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                    ModelState.AddModelError(string.Empty, "Account locked out due to 3 failed attempts. Please try again in 15 minutes.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, $"Invalid login attempt. Remaining attempts before lockout: {3 - user.FailedLoginAttempts}");
                }

                await _db.SaveChangesAsync();
                return View(model);
            }
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}
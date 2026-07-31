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

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                var remainingMinutes = Math.Ceiling((user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes);
                ModelState.AddModelError(string.Empty, $"Account locked. Try again in {remainingMinutes} minutes.");
                return View(model);
            }

            if (user.Hash == model.Password)
            {
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                await _db.SaveChangesAsync();

                string roleName = "CUSTOMER";
                if (user is Admin)
                {
                    roleName = "ADMIN";
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, roleName)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                if (roleName == "ADMIN")
                {
                    return RedirectToAction("Index", "AdminDashboard");
                }
                return RedirectToAction("Index", "Home");
            }
            else
            {
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}
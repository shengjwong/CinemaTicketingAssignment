using System.Security.Claims;
using CinemaTicketingAssignment.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketingAssignment.Controllers
{
    public class AccountController : Controller
    {
        private readonly DB _db;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AccountController(DB db, IPasswordHasher<User> passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        private bool VerifyPassword(User user, string inputPassword)
        {
            if (user.Hash.Trim() == inputPassword.Trim())
            {
                return true;
            }

            try
            {
                var result = _passwordHasher.VerifyHashedPassword(user, user.Hash, inputPassword);
                return result == PasswordVerificationResult.Success;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private async Task HandleFailedLoginAsync(User user, string defaultErrorMessage)
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= 3)
            {
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                ModelState.AddModelError(string.Empty, "Account locked out due to 3 failed attempts. Please try again in 15 minutes.");
            }
            else
            {
                int remainingAttempts = 3 - user.FailedLoginAttempts;
                ModelState.AddModelError(string.Empty, $"{defaultErrorMessage} Remaining attempts before lockout: {remainingAttempts}");
            }

            await _db.SaveChangesAsync();
        }

        // ================= MEMBER LOGIN =================
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

            if (user == null || !(user is Member))
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

            if (VerifyPassword(user, model.Password))
            {
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                await _db.SaveChangesAsync();

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, "MEMBER")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Movies");
            }
            else
            {
                await HandleFailedLoginAsync(user, "Invalid login attempt.");
                return View(model);
            }
        }

        // ================= ADMIN LOGIN =================
        [HttpGet]
        public IActionResult AdminLogin()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || !(user is Admin))
            {
                ModelState.AddModelError(string.Empty, "Invalid attempt or you do not have Admin privileges.");
                return View(model);
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                var remainingMinutes = Math.Ceiling((user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes);
                ModelState.AddModelError(string.Empty, $"Account locked. Try again in {remainingMinutes} minutes.");
                return View(model);
            }

            if (VerifyPassword(user, model.Password))
            {
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                await _db.SaveChangesAsync();

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, "ADMIN")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Admin", "Movies");
            }
            else
            {
                await HandleFailedLoginAsync(user, "Invalid password.");
                return View(model);
            }
        }

        // ================= LOGOUT =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
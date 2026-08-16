using System.Security.Claims;
using CinemaTicketingAssignment.Models;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace CinemaTicketingAssignment.Controllers
{
    public class AccountController : Controller
    {
        private readonly DB _db;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountController(DB db, IPasswordHasher<User> passwordHasher, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        private async Task<bool> IsRecaptchaValidAsync(string recaptchaToken)
        {
            if (string.IsNullOrEmpty(recaptchaToken)) return false;

            var secretKey = _config["RecaptchaSettings:SecretKey"];
            var client = _httpClientFactory.CreateClient();

            var response = await client.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={recaptchaToken}",
                null
            );

            if (!response.IsSuccessStatusCode) return false;

            var jsonString = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(jsonString);
            var root = doc.RootElement;

            bool success = root.GetProperty("success").GetBoolean();
            double score = root.TryGetProperty("score", out var scoreElem) ? scoreElem.GetDouble() : 0.0;

            return success && score >= 0.5;
        }

        private bool VerifyPassword(User user, string inputPassword)
        {
            if (user.Hash.Trim() == inputPassword.Trim()) return true;

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

        // ================= MEMBER REGISTRATION =================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string recaptchaToken)
        {
            if (!await IsRecaptchaValidAsync(recaptchaToken))
            {
                ModelState.AddModelError(string.Empty, "reCAPTCHA verification failed. Please try again.");
                return View(model);
            }

            if (!ModelState.IsValid) return View(model);

            bool emailExists = await _db.Users.AnyAsync(u => u.Email.ToLower() == model.Email.ToLower());
            if (emailExists)
            {
                ModelState.AddModelError("Email", "An account with this email already exists.");
                return View(model);
            }

            var existingMemberIds = await _db.Users
                .Where(u => u.Id.StartsWith("u"))
                .Select(u => u.Id)
                .ToListAsync();

            int maxNum = existingMemberIds
                .Select(id => int.TryParse(id.Substring(1), out int num) ? num : 0)
                .DefaultIfEmpty(100)
                .Max();

            string newId = $"u{maxNum + 1}";

            var newMember = new Member
            {
                Id = newId,
                Name = model.Name.Trim(),
                Email = model.Email.Trim().ToLower(),
                Phone = model.Phone?.Trim() ?? string.Empty,
                PhotoURL = string.Empty,
                FailedLoginAttempts = 0,
                LockoutEnd = null
            };

            newMember.Hash = _passwordHasher.HashPassword(newMember, model.Password);

            _db.Users.Add(newMember);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Registration successful! Please log in with your credentials.";
            return RedirectToAction(nameof(Login));
        }

        // ================= MEMBER LOGIN =================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string recaptchaToken)
        {
            if (!await IsRecaptchaValidAsync(recaptchaToken))
            {
                ModelState.AddModelError(string.Empty, "reCAPTCHA verification failed. Please try again.");
                return View(model);
            }

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
        public async Task<IActionResult> AdminLogin(LoginViewModel model, string recaptchaToken)
        {
            if (!await IsRecaptchaValidAsync(recaptchaToken))
            {
                ModelState.AddModelError(string.Empty, "reCAPTCHA verification failed. Please try again.");
                return View(model);
            }

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

        // ================= FORGOT PASSWORD =================
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());

            if (user != null)
            {
                string token = Guid.NewGuid().ToString();
                user.ResetToken = token;
                user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(30);

                _db.Users.Update(user);
                await _db.SaveChangesAsync();

                var resetLink = Url.Action("ResetPassword", "Account", new { token = token, email = user.Email }, Request.Scheme);

                try
                {
                    var emailMessage = new MimeMessage();
                    emailMessage.From.Add(new MailboxAddress(_config["SmtpSettings:SenderName"], _config["SmtpSettings:SenderEmail"]));
                    emailMessage.To.Add(new MailboxAddress(user.Name, user.Email));
                    emailMessage.Subject = "Reset Your Password - Cinema Ticketing System";

                    emailMessage.Body = new TextPart("html")
                    {
                        Text = $"<p>Hi {user.Name},</p>" +
                               $"<p>You requested a password reset. Click the link below to set a new password:</p>" +
                               $"<p><a href='{resetLink}'>Reset Password</a></p>" +
                               $"<p>This link will expire in 30 minutes.</p>"
                    };

                    using (var client = new SmtpClient())
                    {
                        await client.ConnectAsync(_config["SmtpSettings:Server"], int.Parse(_config["SmtpSettings:Port"]!), MailKit.Security.SecureSocketOptions.StartTls);
                        await client.AuthenticateAsync(_config["SmtpSettings:Username"], _config["SmtpSettings:Password"]);
                        await client.SendAsync(emailMessage);
                        await client.DisconnectAsync(true);
                    }

                    TempData["Success"] = "A password reset link has been sent to your inbox.";
                    return RedirectToAction(nameof(Login));
                }
                catch (Exception ex)
                {
                    TempData["ResetLink"] = resetLink;
                    TempData["ErrorMessage"] = ex.Message;
                    return RedirectToAction(nameof(ForgotPassword));
                }
            }

            ModelState.AddModelError(string.Empty, "No account found with that email address.");
            return View(model);
        }

        // ================= RESET PASSWORD =================
        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction(nameof(Login));
            }

            var model = new ResetPasswordViewModel { Token = token, Email = email };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower() && u.ResetToken == model.Token);

            if (user == null || !user.ResetTokenExpiry.HasValue || user.ResetTokenExpiry.Value < DateTime.UtcNow)
            {
                ModelState.AddModelError(string.Empty, "Invalid or expired password reset token.");
                return View(model);
            }

            user.Hash = _passwordHasher.HashPassword(user, model.NewPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;

            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Your password has been successfully reset. Please log in with your new password.";
            return RedirectToAction(nameof(Login));
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
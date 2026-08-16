using System.Security.Claims;
using CinemaTicketingAssignment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CinemaTicketingAssignment.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly DB _db;
    private readonly IWebHostEnvironment _environment;
    private readonly IPasswordHasher<User> _passwordHasher;

    public ProfileController(DB db, IWebHostEnvironment environment, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _environment = environment;
        _passwordHasher = passwordHasher;
    }

    // GET: /Profile
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _db.Users.FindAsync(userId);

        if (user == null) return NotFound();

        var model = new ProfileViewModel
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            PhotoURL = (user is Member member) ? member.PhotoURL : string.Empty
        };

        return View("Profile", model);
    }

    // POST: /Profile/UpdateProfile
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _db.Users.FindAsync(userId);

        if (user == null) return NotFound();

        ModelState.Remove("CurrentPassword");
        ModelState.Remove("NewPassword");
        ModelState.Remove("ConfirmPassword");

        if (!ModelState.IsValid)
        {
            if (user is Member m) model.PhotoURL = m.PhotoURL;
            return View("Profile", model);
        }

        user.Name = model.Name;
        user.Email = model.Email;
        user.Phone = model.Phone;

        if (user is Member memberUser && model.AvatarFile != null && model.AvatarFile.Length > 0)
        {
            string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.AvatarFile.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.AvatarFile.CopyToAsync(fileStream);
            }

            memberUser.PhotoURL = "/uploads/" + uniqueFileName;
        }

        _db.Users.Update(user);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Profile updated successfully!";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Profile/ChangePassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ProfileViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _db.Users.FindAsync(userId);

        if (user == null) return NotFound();

        if (string.IsNullOrWhiteSpace(model.CurrentPassword) || string.IsNullOrWhiteSpace(model.NewPassword))
        {
            TempData["PasswordError"] = "Please fill in all password fields.";
            return RedirectToAction(nameof(Index));
        }

        if (model.NewPassword != model.ConfirmPassword)
        {
            TempData["PasswordError"] = "New password and confirmation do not match.";
            return RedirectToAction(nameof(Index));
        }

        // 1. Verify Current Password safely (plain text check first + try-catch wrapper)
        bool isCurrentValid = false;

        if (user.Hash.Trim() == model.CurrentPassword.Trim())
        {
            isCurrentValid = true;
        }
        else
        {
            try
            {
                var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.Hash, model.CurrentPassword);
                isCurrentValid = (verificationResult == PasswordVerificationResult.Success);
            }
            catch (FormatException)
            {
                isCurrentValid = false;
            }
        }

        if (!isCurrentValid)
        {
            TempData["PasswordError"] = "Current password is incorrect.";
            return RedirectToAction(nameof(Index));
        }

        // 2. Prevent setting identical password
        if (model.CurrentPassword.Trim() == model.NewPassword.Trim())
        {
            TempData["PasswordError"] = "New password cannot be the same as your current password.";
            return RedirectToAction(nameof(Index));
        }

        // 3. Hash the new password before saving to database
        user.Hash = _passwordHasher.HashPassword(user, model.NewPassword);

        _db.Users.Update(user);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Password updated successfully!";
        return RedirectToAction(nameof(Index));
    }
}
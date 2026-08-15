using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CinemaTicketingAssignment.Models;

public class ProfileViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Phone, MaxLength(20)]
    public string? Phone { get; set; }

    public string PhotoURL { get; set; } = string.Empty;

    public IFormFile? AvatarFile { get; set; }

    // Password Update Fields
    [DataType(DataType.Password)]
    public string? CurrentPassword { get; set; }

    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "New password must be at least 6 characters long.")]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    public string? ConfirmPassword { get; set; }
}
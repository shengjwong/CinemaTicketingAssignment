using System.ComponentModel.DataAnnotations;

namespace CinemaTicketingAssignment.Models;

public abstract class User
{
    [Key, MaxLength(10)]
    public string Id { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Hash { get; set; } = string.Empty;

    // --- NEW FIELDS FOR STUDENT 1 ADVANCED FEATURES ---

    // 1. Temporary login blocking (Account lockout)
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEnd { get; set; }

    // 2. Token-based secure password reset
    [MaxLength(255)]
    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }

    // --------------------------------------------------

    public List<Booking> Bookings { get; set; } = [];
}

public class Admin : User
{
}


public class Member : User
{
    [MaxLength(255)]
    public string PhotoURL { get; set; } = string.Empty;
}
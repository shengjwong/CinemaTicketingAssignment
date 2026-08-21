using System.ComponentModel.DataAnnotations;

namespace CinemaTicketingAssignment.Models;

// Temporary, per-user locks while a customer is completing payment.
public class SeatHold
{
    public int Id { get; set; }

    [MaxLength(10)]
    public string ShowtimeId { get; set; } = string.Empty;

    [MaxLength(6)]
    public string SeatCode { get; set; } = string.Empty;

    [MaxLength(10)]
    public string UserId { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
}

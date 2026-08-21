using System.ComponentModel.DataAnnotations;

namespace CinemaTicketingAssignment.Models;

// A normalized seat record lets SQL Server enforce one seat per showtime.
public class BookingSeat
{
    public int Id { get; set; }

    [MaxLength(10)]
    public string BookingId { get; set; } = string.Empty;
    public Booking Booking { get; set; } = null!;

    [MaxLength(10)]
    public string ShowtimeId { get; set; } = string.Empty;

    [MaxLength(6)]
    public string SeatCode { get; set; } = string.Empty;
}

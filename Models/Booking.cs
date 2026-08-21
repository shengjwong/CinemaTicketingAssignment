using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Add this line

namespace CinemaTicketingAssignment.Models;

public class Booking
{
    [Key, MaxLength(10)]
    public string Id { get; set; } = string.Empty;

    public DateTime BookingDate { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string SelectedSeats { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")] // Add this line
    public decimal TotalAmount { get; set; }

    [MaxLength(30)]
    public string PaymentStatus { get; set; } = "Paid";

    [MaxLength(50)]
    public string PaymentReference { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    public string ShowtimeId { get; set; } = string.Empty;
    public Showtime Showtime { get; set; } = null!;

    public List<BookingSeat> Seats { get; set; } = [];
}

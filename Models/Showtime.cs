using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaTicketingAssignment.Models;

public class Showtime
{
    [Key, MaxLength(10)]
    public string Id { get; set; } = string.Empty;

    public DateTime StartDateTime { get; set; }

    public DateTime EndDateTime { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TicketPrice { get; set; }

    // Foreign Key matching slide standards {Class}{Key}
    public string MovieId { get; set; } = string.Empty;
    public Movie Movie { get; set; } = null!;

    public string CinemaHallId { get; set; } = string.Empty;
    public CinemaHall CinemaHall { get; set; } = null!;

    public List<Booking> Bookings { get; set; } = [];
}
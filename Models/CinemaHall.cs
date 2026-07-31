using System.ComponentModel.DataAnnotations;

namespace CinemaTicketingAssignment.Models;

public class CinemaHall
{
    [Key, MaxLength(10)]
    public string Id { get; set; } = string.Empty;

    // e.g. "Hall 1", "IMAX", "3D"
    [Required, MaxLength(50)]
    public string HallName { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string HallType { get; set; } = string.Empty;

    [Range(1, 1000)]
    public int SeatCapacity { get; set; }

    public List<Showtime> Showtimes { get; set; } = [];
}

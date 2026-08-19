using System.ComponentModel.DataAnnotations;

namespace CinemaTicketingAssignment.Models;

public class Review
{
    [Key, MaxLength(10)]
    public string Id { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
    public int Rating { get; set; }

    [MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign Keys
    public string MovieId { get; set; } = string.Empty;
    public Movie Movie { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;
}
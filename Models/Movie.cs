using System.ComponentModel.DataAnnotations;

namespace CinemaTicketingAssignment.Models;

public enum MovieStatus
{
    NowShowing,
    ComingSoon
}

public class Movie
{
    [Key, MaxLength(10)]
    public string Id { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Synopsis { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Genre { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Language { get; set; } = string.Empty;

    // e.g. U, PG13, 18
    [Required, MaxLength(10)]
    public string AgeClassification { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime ReleaseDate { get; set; }

    public MovieStatus Status { get; set; } = MovieStatus.ComingSoon;

    [MaxLength(255)]
    public string? PosterImagePath { get; set; }

    public List<Showtime> Showtimes { get; set; } = [];
}
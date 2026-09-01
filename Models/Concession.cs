using System.ComponentModel.DataAnnotations;

namespace CinemaTicketingAssignment.Models;

public class Concession
{
    [Key, MaxLength(10)]
    public string Id { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ItemName { get; set; } = string.Empty;

    [Range(0, 1000)]
    [System.ComponentModel.DataAnnotations.Schema.Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Add this line

namespace CinemaTicketingAssignment.Models;

public class Concession
{
    [Key, MaxLength(10)]
    public string Id { get; set; } = string.Empty;

    [MaxLength(100)]
    public string ItemName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")] // Add this line
    public decimal Price { get; set; }
}
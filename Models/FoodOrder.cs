using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CinemaTicketingAssignment.Models;

public class FoodOrder
{
    [Key, MaxLength(12)]
    public string Id { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [MaxLength(30)]
    public string PaymentStatus { get; set; } = "Paid";

    [MaxLength(50)]
    public string PaymentReference { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;

    public List<FoodOrderItem> Items { get; set; } = [];
}

public class FoodOrderItem
{
    public int Id { get; set; }

    [MaxLength(12)]
    public string FoodOrderId { get; set; } = string.Empty;
    public FoodOrder FoodOrder { get; set; } = null!;

    [MaxLength(10)]
    public string ConcessionId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string ItemName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    [NotMapped]
    public decimal Subtotal => UnitPrice * Quantity;
}

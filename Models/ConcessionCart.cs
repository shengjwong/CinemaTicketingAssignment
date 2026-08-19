namespace CinemaTicketingAssignment.Models;

public class ConcessionItemViewModel
{
    public string ConcessionId { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class ConcessionCartViewModel
{
    public List<ConcessionItemViewModel> Items { get; set; } = new();
    public decimal GrandTotal => Items.Sum(i => i.Price * i.Quantity);
}
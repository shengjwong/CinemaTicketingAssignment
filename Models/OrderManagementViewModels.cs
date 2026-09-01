namespace CinemaTicketingAssignment.Models;

public sealed class OrderListItemViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime OrderedAt { get; init; }
    public bool CanCancel =>
        Type == "Booking" &&
        !Status.StartsWith("Cancelled", StringComparison.OrdinalIgnoreCase);
}

public sealed class OrderListViewModel
{
    public List<OrderListItemViewModel> Orders { get; init; } = [];
    public string Search { get; init; } = string.Empty;
    public string Status { get; init; } = "All";
    public string Sort { get; init; } = "date_desc";
    public int Page { get; init; } = 1;
    public int TotalPages { get; init; } = 1;
    public int TotalCount { get; init; }
}

public sealed class OrderDetailViewModel
{
    public OrderListItemViewModel Order { get; init; } = new();
    public List<(string Name, int Quantity, decimal UnitPrice)> Items { get; init; } = [];
    public string? MovieTitle { get; init; }
    public string? HallName { get; init; }
    public DateTime? Showtime { get; init; }
    public string? Seats { get; init; }
}

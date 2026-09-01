using System.Security.Claims;
using CinemaTicketingAssignment.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketingAssignment.Controllers;

public class CartController : Controller
{
    private readonly DB _db;

    public CartController(DB db) => _db = db;

    // ==========================================
    // 1. Cart Page (Public / Member Access)
    // ==========================================
    public IActionResult Index()
    {
        if (User.IsInRole("ADMIN"))
        {
            return Forbid();
        }
        return View();
    }

    // ==========================================
    // 2. Confirm F&B Purchase via AJAX
    // ==========================================
    [HttpPost]
    [Authorize(Roles = "MEMBER")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPurchase([FromBody] List<CartItemRequest> items)
    {
        try
        {
            var order = await CreateFoodOrderAsync(items, "Paid", string.Empty);
            TempData["PaymentSuccess"] = "Payment successful! Your food order has been placed.";
            return Ok(new { redirectUrl = Url.Action(nameof(Receipt), new { id = order.Id }) });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "MEMBER")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPayPalPurchase([FromBody] PayPalFoodOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId))
            return BadRequest(new { message = "A PayPal order ID is required." });

        try
        {
            // Client-ID-only Sandbox demo: the browser captures the PayPal order.
            // Live payments must be verified on the server using a Client Secret.
            var order = await CreateFoodOrderAsync(request.Items, "Paid (PayPal Client Demo)", $"paypal_{request.OrderId}");
            TempData["PaymentSuccess"] = "PayPal Sandbox payment successful! Your food order has been placed.";
            return Ok(new { redirectUrl = Url.Action(nameof(Receipt), new { id = order.Id }) });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ==========================================
    // 3. User Food Order History
    // ==========================================
    [Authorize(Roles = "MEMBER")]
    public async Task<IActionResult> History()
    {
        var userId = CurrentUserId();
        var orders = await _db.FoodOrders
            .Where(o => o.UserId == userId)
            .Include(o => o.Items)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders);
    }

    // ==========================================
    // 4. Food Order Receipt
    // ==========================================
    [Authorize(Roles = "MEMBER")]
    public async Task<IActionResult> Receipt(string id)
    {
        var userId = CurrentUserId();
        var order = await _db.FoodOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

        return order is null ? NotFound() : View(order);
    }

    // ==========================================
    // PRIVATE HELPER METHODS & REQUEST MODELS
    // ==========================================
    private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();

    private async Task<FoodOrder> CreateFoodOrderAsync(List<CartItemRequest>? items, string paymentStatus, string paymentReference)
    {
        if (items is null || items.Count == 0)
            throw new InvalidOperationException("Your cart is empty.");

        var requestedItems = items.Where(i => !string.IsNullOrWhiteSpace(i.Id) && i.Quantity is >= 1 and <= 10)
            .GroupBy(i => i.Id).Select(g => new { Id = g.Key, Quantity = g.Sum(i => i.Quantity) }).ToList();
        if (requestedItems.Count == 0 || requestedItems.Any(i => i.Quantity > 10))
            throw new InvalidOperationException("The cart contains an invalid quantity.");

        var concessions = await _db.Concessions.Where(c => requestedItems.Select(i => i.Id).Contains(c.Id)).ToDictionaryAsync(c => c.Id);
        if (concessions.Count != requestedItems.Count)
            throw new InvalidOperationException("One or more food items are no longer available.");

        var order = new FoodOrder { Id = "FOOD" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(), UserId = CurrentUserId(), OrderDate = DateTime.UtcNow, PaymentStatus = paymentStatus, PaymentReference = paymentReference };
        foreach (var requested in requestedItems)
        {
            var concession = concessions[requested.Id];
            order.Items.Add(new FoodOrderItem { ConcessionId = concession.Id, ItemName = concession.ItemName, UnitPrice = concession.Price, Quantity = requested.Quantity });
        }
        order.TotalAmount = order.Items.Sum(i => i.Subtotal);
        _db.FoodOrders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    public sealed class CartItemRequest
    {
        public string Id { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public sealed class PayPalFoodOrderRequest
    {
        public List<CartItemRequest> Items { get; set; } = [];
        public string OrderId { get; set; } = string.Empty;
    }
}

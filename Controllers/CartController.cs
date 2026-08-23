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

    public IActionResult Index()
    {
        if (User.IsInRole("ADMIN"))
        {
            return Forbid();
        }
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "MEMBER")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPurchase([FromBody] List<CartItemRequest> items)
    {
        if (items is null || items.Count == 0)
            return BadRequest(new { message = "Your cart is empty." });

        var requestedItems = items
            .Where(i => !string.IsNullOrWhiteSpace(i.Id) && i.Quantity is >= 1 and <= 10)
            .GroupBy(i => i.Id)
            .Select(g => new { Id = g.Key, Quantity = g.Sum(i => i.Quantity) })
            .ToList();

        if (requestedItems.Count == 0 || requestedItems.Any(i => i.Quantity > 10))
            return BadRequest(new { message = "The cart contains an invalid quantity." });

        var concessions = await _db.Concessions
            .Where(c => requestedItems.Select(i => i.Id).Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);

        if (concessions.Count != requestedItems.Count)
            return BadRequest(new { message = "One or more food items are no longer available." });

        var order = new FoodOrder
        {
            Id = "FOOD" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            OrderDate = DateTime.UtcNow,
            PaymentStatus = "Paid"
        };

        foreach (var requested in requestedItems)
        {
            var concession = concessions[requested.Id];
            order.Items.Add(new FoodOrderItem
            {
                ConcessionId = concession.Id,
                ItemName = concession.ItemName,
                UnitPrice = concession.Price,
                Quantity = requested.Quantity
            });
        }

        order.TotalAmount = order.Items.Sum(i => i.Subtotal);
        _db.FoodOrders.Add(order);
        await _db.SaveChangesAsync();

        return Ok(new { redirectUrl = Url.Action(nameof(Receipt), new { id = order.Id }) });
    }

    [Authorize(Roles = "MEMBER")]
    public async Task<IActionResult> History()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var orders = await _db.FoodOrders
            .Where(o => o.UserId == userId)
            .Include(o => o.Items)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders);
    }

    [Authorize(Roles = "MEMBER")]
    public async Task<IActionResult> Receipt(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var order = await _db.FoodOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

        return order is null ? NotFound() : View(order);
    }

    public sealed class CartItemRequest
    {
        public string Id { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}
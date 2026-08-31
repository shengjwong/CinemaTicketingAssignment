using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CinemaTicketingAssignment.Models;

namespace CinemaTicketingAssignment.Controllers;

public class ConcessionsController : Controller
{
    private readonly DB _db;

    public ConcessionsController(DB db)
    {
        _db = db;
    }

    // ==========================================
    // 1. 用户浏览加购页面 (Public / Member Access)
    // ==========================================
    public async Task<IActionResult> Index()
    {
        var concessions = await _db.Concessions.ToListAsync();
        return View(concessions);
    }

    // ==========================================
    // 2. Admin 餐饮菜单管理列表
    // ==========================================
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Manage()
    {
        var concessions = await _db.Concessions.ToListAsync();
        return View(concessions);
    }

    // ==========================================
    // 3. Admin 新建小吃
    // ==========================================
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string itemName, decimal price)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        if (string.IsNullOrWhiteSpace(itemName) || price < 0)
        {
            if (isAjax) return BadRequest(new { message = "Please provide a valid item name and price." });
            TempData["Error"] = "Please provide a valid item name and price.";
            return RedirectToAction(nameof(Manage));
        }

        var item = new Concession
        {
            Id = "CON" + Guid.NewGuid().ToString("N")[..7].ToUpper(),
            ItemName = itemName,
            Price = price
        };

        _db.Concessions.Add(item);
        await _db.SaveChangesAsync();

        if (isAjax)
        {
            return Ok(new { id = item.Id, itemName = item.ItemName, price = item.Price.ToString("0.00") });
        }

        TempData["Success"] = "New snack added successfully!";
        return RedirectToAction(nameof(Manage));
    }

    // ==========================================
    // 4. Admin 删除小吃
    // ==========================================
    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        var item = await _db.Concessions.FindAsync(id);
        if (item != null)
        {
            _db.Concessions.Remove(item);
            await _db.SaveChangesAsync();

            if (isAjax) return Ok(new { id });
            TempData["Success"] = "Snack item deleted.";
        }
        else if (isAjax)
        {
            return NotFound(new { message = "Snack item not found." });
        }

        return RedirectToAction(nameof(Manage));
    }
}
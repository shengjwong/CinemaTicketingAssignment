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

    // 用户浏览加购页面
    public async Task<IActionResult> Index()
    {
        var concessions = await _db.Concessions.ToListAsync();
        return View(concessions);
    }

    // Admin 餐饮菜单管理列表
    public async Task<IActionResult> Manage()
    {
        if (!User.IsInRole("ADMIN")) return Forbid();
        var concessions = await _db.Concessions.ToListAsync();
        return View(concessions);
    }

    // Admin 新建小吃
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string itemName, decimal price)
    {
        if (!User.IsInRole("ADMIN")) return Forbid();

        var item = new Concession
        {
            Id = "CON" + Guid.NewGuid().ToString("N")[..7].ToUpper(),
            ItemName = itemName,
            Price = price
        };
        _db.Concessions.Add(item);
        await _db.SaveChangesAsync();
        TempData["Success"] = "New snack added successfully!";
        return RedirectToAction("Manage");
    }

    // Admin 删除小吃
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (!User.IsInRole("ADMIN")) return Forbid();
        var item = await _db.Concessions.FindAsync(id);
        if (item != null)
        {
            _db.Concessions.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Snack item deleted.";
        }
        return RedirectToAction("Manage");
    }
}
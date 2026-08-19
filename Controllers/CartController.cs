using Microsoft.AspNetCore.Mvc;

namespace CinemaTicketingAssignment.Controllers;

public class CartController : Controller
{
    public IActionResult Index()
    {
        if (User.IsInRole("ADMIN"))
        {
            return Forbid();
        }
        return View();
    }
}
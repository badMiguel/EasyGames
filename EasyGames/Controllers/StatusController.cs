using EasyGames.Data;
using EasyGames.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EasyGames.Controllers;

[Authorize]
public class StatusController : Controller
{
    private readonly EasyGamesContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public StatusController(EasyGamesContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /Status
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        int points = user.AccountPoints;

        // derive tier from points (using your existing constants in Models/User.cs)
        string tier = UserStatus.Unranked;
        if (points >= StatusPoints.Platinum) tier = UserStatus.Platinum;
        else if (points >= StatusPoints.Gold) tier = UserStatus.Gold;
        else if (points >= StatusPoints.Silver) tier = UserStatus.Silver;
        else if (points >= StatusPoints.Bronze) tier = UserStatus.Bronze;

        // recent orders (top 3)
        var recentOrders = await _context.Order
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Inventory)
                .ThenInclude(inv => inv.Item)
            .Include(o => o.Customer)
            .Where(o => o.Status == OrderStatus.Ordered && o.Customer.UserId == user.Id)
            .OrderByDescending(o => o.OrderDate)
            .Take(3)
            .ToListAsync();

        var model = new AccountStatusViewModel
        {
            Points = points,
            Tier = tier,
            RecentOrders = recentOrders
        };

        return View(model);
    }
}

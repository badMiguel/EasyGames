using System.Text.Encodings.Web;
using EasyGames.Data;
using EasyGames.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EasyGames.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly EasyGamesContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(EasyGamesContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Transactions()
    {
        var loggedInUserId = _userManager.GetUserId(User);

        var orders = await _context
            .Order.Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Inventory)
            .ThenInclude(i => i.Item)
            .Where(o => o.Customer.UserId == loggedInUserId && o.Status == OrderStatus.Ordered)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        var transactions = orders
            .Select(o => new UserTransactionViewModel
            {
                Order = o,
                OrderId = o.OrderId,
                OrderItems = o.OrderItems,
                TotalCost = o.OrderItems.Sum(oi => oi.UnitPrice * oi.Quantity),
            })
            .ToList();

        return View(transactions);
    }
}

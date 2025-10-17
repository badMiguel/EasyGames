// Used ChatGPT to generate this code

using System.Runtime.InteropServices.Marshalling;
using System.Text.Encodings.Web;
using EasyGames.Data;
using EasyGames.Models;
using EasyGames.Utilities;
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

    public async Task<IActionResult> Transactions(int? pageNumber, int? pageSize)
    {
        var loggedInUserId = _userManager.GetUserId(User);

        var orders = _context
            .Order.Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Inventory)
            .ThenInclude(i => i.Item)
            .Where(o => o.Customer.UserId == loggedInUserId && o.Status == OrderStatus.Ordered)
            .OrderByDescending(o => o.OrderDate)
            .AsNoTracking();

        var paginatedOrders = await Pagination<Order>.CreateAsync(orders, pageNumber, pageSize);
        ViewData["PageDetails"] = new PageDetails
        {
            PageSize = paginatedOrders.PageSize,
            PageIndex = paginatedOrders.PageIndex,
            HasNextPage = paginatedOrders.HasNextPage,
            HasPreviousPage = paginatedOrders.HasPreviousPage,
        };

        var transactions = paginatedOrders
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

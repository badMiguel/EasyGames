using System.Text.Encodings.Web;
using EasyGames.Data;
using EasyGames.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EasyGames.Controllers;

public class UserSales : Controller
{
    private readonly EasyGamesContext _context;

    public UserSales(EasyGamesContext context)
    {
        _context = context;
    }

    private decimal GetTotalRevenue(IEnumerable<UserTransactionViewModel> transactions)
    {
        decimal totalRevenue = 0;
        foreach (var transaction in transactions)
        {
            totalRevenue += transaction.OrderItems.Sum(oi => oi.Quantity * oi.UnitPrice);
        }
        return totalRevenue;
    }

    private decimal GetTotalProfit(IEnumerable<UserTransactionViewModel> transactions)
    {
        decimal totalProfit = 0;
        foreach (var transaction in transactions)
        {
            totalProfit += transaction.OrderItems.Sum(oi => oi.Quantity * (oi.UnitPrice - oi.UnitBuyPrice));
        }
        return totalProfit;
    }

    public IActionResult Index()
    {
        var transactions = _context
            .Order.Include(o => o.Customer)
            .ThenInclude(c => c.User)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Inventory)
            .ThenInclude(i => i.Item)
            .Where(o => o.Status == OrderStatus.Ordered)
            .Select(o => new UserTransactionViewModel
            {
                Order = o,
                OrderId = o.OrderId,
                OrderItems = o.OrderItems,
                TotalCost = o.OrderItems.Sum(oi => oi.UnitPrice * oi.Quantity),
            })
            .ToList();

        var userSales = new UserSalesViewModel
        {
            TotalRevenue = GetTotalRevenue(transactions),
            TotalProfit = GetTotalProfit(transactions),
            Transactions = transactions,
        };

        return View(userSales);
    }
}

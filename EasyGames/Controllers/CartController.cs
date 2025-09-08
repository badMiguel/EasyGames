using EasyGames.Data;
using EasyGames.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EasyGames.Controllers;

public class CartController : Controller
{
    private readonly EasyGamesContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CartController(UserManager<ApplicationUser> userManager, EasyGamesContext context)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> AddToCart(ItemDetails itemDetails, int quantity)
    {
        var orderId = await GetUserOrderId();
        var unitPrice = await GetUnitPrice(itemDetails.ItemId) ?? -1;
        // price cannot be null, raise an error
        if (unitPrice <= -1)
        {
            return RedirectToAction("Error", "Home");
        }

        await _context.OrderItem.AddAsync(
            new OrderItem
            {
                OrderId = orderId,
                ItemId = itemDetails.ItemId,
                UnitPrice = unitPrice,
                Quantity = quantity,
            }
        );
        await _context.SaveChangesAsync();

        return RedirectToAction("ItemDetails", "Home", new { id = itemDetails.ItemId });
    }

    private async Task<decimal?> GetUnitPrice(int id)
    {
        var item = await _context.Item.FindAsync(id);
        if (item == null)
        {
            return null;
        }
        return item.Price;
    }

    private async Task<int> GetUserOrderId()
    {
        var loggedInUserId = _userManager.GetUserId(User);
        var order = _context.Order.FirstOrDefault(o =>
            o.Status == OrderStatus.InCart || o.UserId == loggedInUserId
        );

        if (order != null)
        {
            return order.OrderId;
        }

        var newOrder = new Order { UserId = loggedInUserId, Status = OrderStatus.InCart };
        await _context.Order.AddAsync(newOrder);
        await _context.SaveChangesAsync();
        return newOrder.OrderId;
    }
}

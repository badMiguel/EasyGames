using EasyGames.Data;
using EasyGames.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        var orderItemExists = await OrderItemExists(itemDetails, quantity);
        if (orderItemExists)
        {
            return RedirectToAction("ItemDetails", "Home", new { id = itemDetails.ItemId });
        }

        var orderId = await GetUserOrderId();
        var unitPrice = await GetUnitPrice(itemDetails.ItemId) ?? -1;
        // price cannot be null, raise an error
        if (unitPrice <= -1)
        {
            return RedirectToAction("Error", "Home");
        }
        await CreateOrderItem(itemDetails, quantity, orderId, unitPrice);
        return RedirectToAction("ItemDetails", "Home", new { id = itemDetails.ItemId });
    }

    private async Task<bool> OrderItemExists(ItemDetails itemDetails, int quantity)
    {
        var orderItem = await _context.OrderItem.FirstOrDefaultAsync(oi =>
            oi.ItemId == itemDetails.ItemId
        );

        if (orderItem == null)
        {
            return false;
        }

        orderItem.Quantity += quantity;
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task CreateOrderItem(
        ItemDetails itemDetails,
        int quantity,
        int orderId,
        decimal unitPrice
    )
    {
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

    public async Task<IActionResult> BuyNow(ItemDetails itemDetails, int quantity)
    {
        var orderItemExists = await OrderItemExists(itemDetails, quantity);
        if (orderItemExists)
        {
            return RedirectToAction("ViewCart", "Cart", new { id = itemDetails.ItemId });
        }

        var orderId = await GetUserOrderId();
        var unitPrice = await GetUnitPrice(itemDetails.ItemId) ?? -1;
        // price cannot be null, raise an error
        if (unitPrice <= -1)
        {
            return RedirectToAction("Error", "Home");
        }
        await CreateOrderItem(itemDetails, quantity, orderId, unitPrice);
        return RedirectToAction("ViewCart");
    }

    public async Task<IActionResult> ViewCart()
    {
        int orderId = await GetUserOrderId();
        var orderItems = _context
            .OrderItem.Include(oi => oi.Item)
            .ThenInclude(i => i.ItemCategorys)
            .ThenInclude(ic => ic.Category)
            .Where(oi => oi.OrderId == orderId)
            .ToList();

        return View(orderItems);
    }

    public async Task<IActionResult> ChangeQuantity(OrderItem formOrderItem)
    {
        var orderItem = await _context.OrderItem.FindAsync(formOrderItem.OrderItemId);

        if (orderItem != null)
        {
            orderItem.Quantity = formOrderItem.Quantity;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("ViewCart");
    }

    public async Task<IActionResult> RemoveItem(int orderItemId)
    {
        var orderItem = await _context.OrderItem.FindAsync(orderItemId);

        if (orderItem != null)
        {
            _context.OrderItem.Remove(orderItem);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("ViewCart");
    }

    public IActionResult PlaceOrder()
    {
        return RedirectToAction("OrderSummary");
    }

    public IActionResult OrderSummary()
    {
        return View();
    }
}

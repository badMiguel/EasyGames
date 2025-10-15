using EasyGames.Data;
using EasyGames.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace EasyGames.Controllers;

// [Authorize]
public class CartController : Controller
{
    private readonly EasyGamesContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string GuestCustomerSessionKey = "GuestCustomerId";

    public CartController(
        UserManager<ApplicationUser> userManager,
        EasyGamesContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(ItemDetailsUserViewModel itemDetails, int quantity)
    {
        var orderItemExists = await OrderItemExists(itemDetails, quantity);
        if (orderItemExists)
        {
            return RedirectToAction("ItemDetails", "Home", new { id = itemDetails.ItemId });
        }

        var orderId = await GetUserOrderId();
        var unitPrice = await GetUnitPrice(itemDetails.Inventory.InventoryId) ?? -1;
        // price cannot be null, raise an error
        if (unitPrice <= -1)
        {
            return RedirectToAction("Error", "Home");
        }
        var unitBuyPrice = await GetUnitBuyPrice(itemDetails.ItemId) ?? -1;
        // price cannot be null, raise an error
        if (unitBuyPrice <= -1)
        {
            return RedirectToAction("Error", "Home");
        }
        await CreateOrderItem(itemDetails, quantity, orderId, unitPrice, unitBuyPrice);
        return RedirectToAction("ItemDetails", "Home", new { id = itemDetails.ItemId });
    }

    private async Task<Shop?> GetOnlineShop()
    {
        return await _context
            .Shop.Include(s => s.Inventories)
            .FirstOrDefaultAsync(s => s.LocationType == LocationTypes.Online);
    }

    private async Task<Customer?> GetCustomer()
    {
        var loggedInUserId = _userManager.GetUserId(User);
        if (loggedInUserId != null)
        {
            var getCustomer = await _context.Customer.FirstOrDefaultAsync(c =>
                c.UserId == loggedInUserId
            );
            return getCustomer;
        }
        return null;
    }

    private async Task<Customer> GetOrCreateCustomerAsync()
    {
        // If logged in, use existing logic
        var loggedInUserId = _userManager.GetUserId(User);
        if (!string.IsNullOrEmpty(loggedInUserId))
        {
            var existing = await _context.Customer.FirstOrDefaultAsync(c => c.UserId == loggedInUserId);
            if (existing == null)
            {
                existing = new Customer
                {
                    UserId = loggedInUserId,
                    IsGuest = false
                };
                _context.Customer.Add(existing);
                await _context.SaveChangesAsync();
            }
            return existing;
        }

        // Guest path
        var session = _httpContextAccessor.HttpContext!.Session;
        var guestId = session.GetInt32(GuestCustomerSessionKey);
        Customer? guest = null;

        if (guestId.HasValue)
        {
            guest = await _context.Customer.FindAsync(guestId.Value);
        }

        if (guest == null)
        {
            guest = new Customer
            {
                IsGuest = true
            };
            _context.Customer.Add(guest);
            await _context.SaveChangesAsync();
            session.SetInt32(GuestCustomerSessionKey, guest.CustomerId);
        }

        return guest;
    }


    private async Task<bool> OrderItemExists(ItemDetailsUserViewModel itemDetails, int quantity)
    {
        var customer = await GetCustomer();
        if (customer == null)
        {
            return false;
        }

        var inventory = await _context.Inventory.FirstOrDefaultAsync(i =>
            i.InventoryId == itemDetails.Inventory.InventoryId
        );

        var orderItem = await _context
            .OrderItem.Include(oi => oi.Order)
            .ThenInclude(o => o.Customer)
            .Include(oi => oi.Inventory)
            .FirstOrDefaultAsync(oi =>
                oi.Inventory.ItemId == inventory.ItemId
                && oi.Order.Status == OrderStatus.InCart
                && oi.Order.CustomerId == customer.CustomerId
            );

        if (orderItem == null)
        {
            return false;
        }

        var newQuantity = orderItem.Quantity + quantity;
        if (newQuantity <= orderItem.Inventory.Quantity)
        {
            orderItem.Quantity = newQuantity;
        }
        else
        {
            orderItem.Quantity = orderItem.Inventory.Quantity;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    private async Task CreateOrderItem(
        ItemDetailsUserViewModel itemDetails,
        int quantity,
        int orderId,
        decimal unitPrice,
        decimal unitBuyPrice
    )
    {
        await _context.OrderItem.AddAsync(
            new OrderItem
            {
                OrderId = orderId,
                InventoryId = itemDetails.Inventory.InventoryId,
                UnitPrice = unitPrice,
                UnitBuyPrice = unitBuyPrice,
                Quantity = quantity,
            }
        );
        await _context.SaveChangesAsync();
    }

    private async Task<decimal?> GetUnitPrice(int id)
    {
        var inventory = await _context.Inventory.FindAsync(id);
        if (inventory == null)
        {
            return null;
        }
        return inventory.SellPrice;
    }

    private async Task<decimal?> GetUnitBuyPrice(int id)
    {
        var item = await _context.Item.FindAsync(id);
        if (item == null)
        {
            return null;
        }
        return item.BuyPrice;
    }


    private async Task<int> GetUserOrderId()
    {
        // Use the new helper (works for logged-in AND guest)
        var customer = await GetOrCreateCustomerAsync();

        // Ensure there is an Online shop to attach the cart to
        var shop = await GetOnlineShop();
        if (shop == null)
        {
            shop = new Shop
            {
                ShopName = "Web Storefront",
                ContactNumber = "0000 000 000", // placeholder to satisfy [Required]
                LocationType = LocationTypes.Online
            };
            _context.Shop.Add(shop);
            await _context.SaveChangesAsync();
        }

        // Reuse an existing "in cart" order or create a new one
        var order = await _context.Order
            .FirstOrDefaultAsync(o => o.Status == OrderStatus.InCart
                                   && o.CustomerId == customer.CustomerId);

        if (order != null)
        {
            return order.OrderId;
        }

        var newOrder = new Order
        {
            CustomerId = customer.CustomerId,
            ShopId = shop.ShopId,
            Status = OrderStatus.InCart
        };
        _context.Order.Add(newOrder);
        await _context.SaveChangesAsync();

        return newOrder.OrderId;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BuyNow(ItemDetailsUserViewModel itemDetails, int quantity)
    {
        var orderItemExists = await OrderItemExists(itemDetails, quantity);
        if (orderItemExists)
        {
            return RedirectToAction("ViewCart", "Cart", new { id = itemDetails.ItemId });
        }

        var orderId = await GetUserOrderId();
        var unitPrice = await GetUnitPrice(itemDetails.Inventory.InventoryId) ?? -1;
        // price cannot be null, raise an error
        if (unitPrice <= -1)
        {
            return RedirectToAction("Error", "Home");
        }
        var unitBuyPrice = await GetUnitBuyPrice(itemDetails.ItemId) ?? -1;
        // price cannot be null, raise an error
        if (unitBuyPrice <= -1)
        {
            return RedirectToAction("Error", "Home");
        }
        await CreateOrderItem(itemDetails, quantity, orderId, unitPrice, unitBuyPrice);
        return RedirectToAction("ViewCart");
    }

    public async Task<IActionResult> ViewCart()
    {
        // bool isLoggedIn = User.Identity?.IsAuthenticated ?? false;

        // if (!isLoggedIn)
        // {
        //     return RedirectToPage("/Account/Login", new { area = "Identity" });
        // }

        int orderId = await GetUserOrderId();
        var orderItems = GetOrderedItems(orderId);
        ViewData["OrderId"] = orderId;
        ViewData["OrderItemError"] = TempData["OrderItemError"];
        return View(orderItems);
    }

    private List<OrderItem> GetOrderedItems(int orderId)
    {
        return _context
            .OrderItem.Include(oi => oi.Inventory)
            .ThenInclude(inv => inv.Item)
            .ThenInclude(item => item.ItemCategorys)
            .ThenInclude(ic => ic.Category)
            .Where(oi => oi.OrderId == orderId)
            .ToList();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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

    [HttpPost]
    [ValidateAntiForgeryToken]
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

    private bool ValidateOrder(int orderId)
    {
        var orderItems = GetOrderedItems(orderId);
        foreach (var orderItem in orderItems)
        {
            if (orderItem.Quantity > orderItem.Inventory.Quantity)
            {
                return false;
            }
        }
        return true;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(int orderId)
    {
        if (!ValidateOrder(orderId))
        {
            TempData["OrderItemError"] = "Sorry there are not enough stock left for this item.";
            return RedirectToAction("ViewCart");
        }

        var order = await _context.Order.FindAsync(orderId);
        if (order != null)
        {
            order.Status = OrderStatus.Ordered;
            order.OrderDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await DecrementStockAmount(orderId);

            TempData["OrderId"] = orderId;
            return RedirectToAction("OrderSummary");
        }

        return RedirectToAction("ViewCart");
    }

    private async Task DecrementStockAmount(int orderId)
    {
        var orderItems = GetOrderedItems(orderId);
        if (orderItems != null)
        {
            foreach (var orderItem in orderItems)
            {
                orderItem.Inventory.Quantity -= orderItem.Quantity;
            }
            await _context.SaveChangesAsync();
        }
    }

    public IActionResult OrderSummary()
    {
        var orderId = TempData["OrderId"] as int? ?? -1;
        if (orderId <= -1)
        {
            return RedirectToAction("ViewCart");
        }

        var orderedItems = GetOrderedItems(orderId);
        ViewData["OrderId"] = orderId;
        return View(orderedItems);
    }
}

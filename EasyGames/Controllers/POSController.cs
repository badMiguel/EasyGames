using System.Security.Claims;
using EasyGames.Data;
using EasyGames.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EasyGames.Controllers
{
    [Route("Shop/{shopId}/POS")]
    [Authorize(Roles = UserRoles.ShopProprietor)]
    public class PosController : Controller
    {
        private readonly EasyGamesContext _context;

        public PosController(EasyGamesContext context)
        {
            _context = context;
        }

        private async Task<bool> IsOwnerOfShopAsync(int? shopId)
        {
            if (shopId == null) return false;
            if (User.IsInRole(UserRoles.Owner)) return true;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var shop = await _context.Shop.AsNoTracking().FirstOrDefaultAsync(s => s.ShopId == shopId);
            return shop?.OwnerId == userId;
        }

        #region CartSessionHelpers
        private List<OrderItem> GetCart()
        {
            var json = HttpContext.Session.GetString("PosCart");
            return string.IsNullOrEmpty(json)
                ? new List<OrderItem>()
                : System.Text.Json.JsonSerializer.Deserialize<List<OrderItem>>(json) ?? new List<OrderItem>();
        }

        private void SaveCart(List<OrderItem> cart)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString("PosCart", json);
        }

        private void ClearCartSession()
        {
            HttpContext.Session.Remove("PosCart");
        }
        #endregion

        [HttpGet("")]
        public async Task<IActionResult> Index(int shopId)
        {
            if (!await IsOwnerOfShopAsync(shopId))
                return Forbid();

            var shop = await _context.Shop.FindAsync(shopId);
            if (shop == null)
                return NotFound();

            if (shop.LocationType != LocationTypes.Physical)
            {
                TempData["Error"] = "This POS is only available for physical shops.";
                return RedirectToAction("Index", "Shop");
            }

            var inventoryList = await _context.Inventory
                .Include(i => i.Item)
                    .ThenInclude(it => it.ItemCategorys)
                        .ThenInclude(ic => ic.Category)
                .Where(i => i.ShopId == shopId)
                .OrderBy(i => i.Item!.Name)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.ShopName = shop.ShopName;
            ViewBag.ShopId = shopId;
            ViewBag.Cart = GetCart();

            var categories = await _context.Category
                .OrderBy(c => c.Name)
                .AsNoTracking()
                .ToListAsync();
            ViewBag.Categories = categories;

            return View(inventoryList);
        }

  
        [HttpPost("AddToCart")]
        [ActionName("AddToCartAjax")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCartAjax(int shopId, int inventoryId, int quantity = 1)
        {
            if (!await IsOwnerOfShopAsync(shopId))
                return Forbid();

            var inventory = await _context.Inventory
                .Include(i => i.Item)
                .FirstOrDefaultAsync(i => i.InventoryId == inventoryId && i.ShopId == shopId);

            if (inventory == null)
                return NotFound();

            var cart = GetCart();
            var existing = cart.FirstOrDefault(c => c.InventoryId == inventoryId);

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else if (inventory.Item != null)
                {
        
                    cart.Add(new OrderItem
                {
                    InventoryId = inventory.InventoryId,
                    Quantity = quantity,
                    UnitPrice = inventory.SellPrice,
                    UnitBuyPrice = inventory.Item.BuyPrice,
                    DiscountPercent = 0.0000M
                });
            }

            SaveCart(cart);

            var subtotal = cart.Sum(c => c.Quantity * c.UnitPrice);

            return Json(new
            {
                success = true,
                cartCount = cart.Count,
                subtotal = subtotal.ToString("C"),
                items = cart.Select(c => new
                {
                    inventoryId = c.InventoryId,
                    itemName = _context.Inventory
                        .Include(i => i.Item)
                        .Where(i => i.InventoryId == c.InventoryId)
                        .Select(i => i.Item!.Name)
                        .FirstOrDefault() ?? "Unknown",
                    quantity = c.Quantity,
                    unitPrice = c.UnitPrice.ToString("C"),
                    subtotal = (c.Quantity * c.UnitPrice).ToString("C")
                })
            });
        }

        [HttpPost("RemoveFromCart")]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(int shopId, int inventoryId)
        {
            var cart = GetCart();
            cart.RemoveAll(c => c.InventoryId == inventoryId);
            SaveCart(cart);
            TempData["Success"] = "Item removed from sale cart.";
            return RedirectToAction(nameof(Index), new { shopId });
        }

        [HttpPost("ClearCart")]
        [ValidateAntiForgeryToken]
        public IActionResult ClearCart(int shopId)
        {
            ClearCartSession();
            TempData["Success"] = "Sale cart cleared.";
            return RedirectToAction(nameof(Index), new { shopId });
        }

        [HttpPost("Checkout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int shopId, int? customerId)
        {
            if (!await IsOwnerOfShopAsync(shopId))
                return Forbid();

            var shop = await _context.Shop.FindAsync(shopId);
            if (shop == null)
                return NotFound();

            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["Error"] = "Cart is empty. Add items before checkout.";
                return RedirectToAction(nameof(Index), new { shopId });
            }

            Customer? customer = null;

            if (customerId.HasValue)
            {
                customer = await _context.Customer.FindAsync(customerId.Value);
            }

            if (customer == null)
            {
                customer = new Customer
                {
                    IsGuest = true,
                };
                _context.Customer.Add(customer);
                await _context.SaveChangesAsync();
            }

            var order = new Order
            {
                CustomerId = customer.CustomerId,
                ShopId = shop.ShopId,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Ordered
            };

            _context.Order.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cart)
            {
                var inventory = await _context.Inventory
                    .Include(i => i.Item)
                    .FirstOrDefaultAsync(i => i.InventoryId == item.InventoryId);

                if (inventory == null)
                    continue;

                if (inventory.Item == null)
                    continue;

                if (inventory.Quantity < item.Quantity)
                    continue;

                var orderItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    InventoryId = inventory.InventoryId,
                    Quantity = item.Quantity,
                    UnitPrice = inventory.SellPrice,
                    UnitBuyPrice = inventory.Item.BuyPrice,
                    DiscountPercent = item.DiscountPercent
                };

                _context.OrderItem.Add(orderItem);

                inventory.Quantity -= item.Quantity;
            }

            await _context.SaveChangesAsync();


            ClearCartSession();

            return RedirectToAction(nameof(Receipt), new { shopId = shop.ShopId, orderId = order.OrderId });

        }

        [HttpPost("RemoveOne")]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveOne(int shopId, int inventoryId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.InventoryId == inventoryId);

            if (item != null)
            {
                item.Quantity -= 1;
                if (item.Quantity <= 0)
                    cart.Remove(item);
            }

            SaveCart(cart);

            var subtotal = cart.Sum(c => c.Quantity * c.UnitPrice);

            var items = cart.Select(c => new
            {
                inventoryId = c.InventoryId,
                itemName = _context.Inventory
                    .Include(i => i.Item)
                    .Where(i => i.InventoryId == c.InventoryId)
                    .Select(i => i.Item!.Name)
                    .FirstOrDefault() ?? "Unknown",
                quantity = c.Quantity,
                unitPrice = c.UnitPrice.ToString("C"),
                subtotal = (c.Quantity * c.UnitPrice).ToString("C")
            });

            return Json(new
            {
                success = true,
                cartCount = cart.Sum(c => c.Quantity),
                subtotal = subtotal.ToString("C"),
                items
            });
        }

        [HttpGet("SearchCustomer")]
        public async Task<IActionResult> SearchCustomer(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Json(new List<object>());

            var customersQuery = _context.Customer
                .Include(c => c.User)
                .Where(c =>
                    (c.User != null && (
                        c.User.Email.Contains(query) ||
                        c.User.UserName.Contains(query) ||
                        c.User.PhoneNumber.Contains(query)
                    )) ||
                    (!c.IsGuest && query.ToLower() == "guest")
                );

            var customersList = await customersQuery.ToListAsync();

            var customers = customersList.Select(c => new
            {
                c.CustomerId,
                Name = c.User != null ? c.User.UserName : "Guest",
                Email = c.User != null ? c.User.Email : null,
                Phone = c.User != null ? c.User.PhoneNumber : null,
                Points = c.User != null ? c.User.AccountPoints : 0,
                Status = GetStatusName(c.User != null ? c.User.AccountPoints : 0)
            }).Take(10).ToList();

            return Json(customers);
        }

        private string GetStatusName(int points)
        {
            if (points >= StatusPoints.Platinum) return UserStatus.Platinum;
            if (points >= StatusPoints.Gold) return UserStatus.Gold;
            if (points >= StatusPoints.Silver) return UserStatus.Silver;
            if (points >= StatusPoints.Bronze) return UserStatus.Bronze;
            return UserStatus.Unranked;
        }



        [HttpGet("Receipt/{orderId}")]
        public async Task<IActionResult> Receipt(int shopId, int orderId)
        {
            var order = await _context.Order
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Inventory)
                        .ThenInclude(i => i.Item)
                .Include(o => o.Customer)
                .Include(o => o.Shop)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.ShopId == shopId);

            if (order == null)
                return NotFound();

            return View(order);
        }

    }
}


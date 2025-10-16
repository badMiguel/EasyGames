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
        // Helper to check if the current user is the owner of the shop
        private async Task<bool> IsOwnerOfShopAsync(int? shopId)
        {
            if (shopId == null) return false;
            // Owners have access to all shops
            if (User.IsInRole(UserRoles.Owner)) return true;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var shop = await _context.Shop.AsNoTracking().FirstOrDefaultAsync(s => s.ShopId == shopId);
            return shop?.OwnerId == userId;
        }
       
        #region CartSessionHelpers
        // Cart management using session storage
        private List<OrderItem> GetCart()
        {
            var json = HttpContext.Session.GetString("PosCart");
            return string.IsNullOrEmpty(json)
                ? new List<OrderItem>()
                : System.Text.Json.JsonSerializer.Deserialize<List<OrderItem>>(json) ?? new List<OrderItem>();
        }
        // Saves the cart to session storage
        private void SaveCart(List<OrderItem> cart)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString("PosCart", json);
        }

        // Clears the cart from session storage
        private void ClearCartSession()
        {
            HttpContext.Session.Remove("PosCart");
        }
        #endregion

        // GET: Shop/{shopId}/POS
        [HttpGet("")]
        public async Task<IActionResult> Index(int shopId)
        {
            if (!await IsOwnerOfShopAsync(shopId))
                return Forbid();

            var shop = await _context.Shop.FindAsync(shopId);
            if (shop == null)
                return NotFound();

            // POS is only for physical shops
            if (shop.LocationType != LocationTypes.Physical)
            {
                TempData["Error"] = "POS is only available for physical shops.";
                return RedirectToAction("Index", "Shop");
            }

            // Load inventory with related item and category data
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

            // Load categories for filtering
            var categories = await _context.Category
                .OrderBy(c => c.Name)
                .AsNoTracking()
                .ToListAsync();
            ViewBag.Categories = categories;

            return View(inventoryList);
        }

        // AJAX: Add item to cart
        [HttpPost("AddToCart")]
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
                    // Add new item to cart session
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

            // Get item names for the cart items
            var names = await _context.Inventory
                .Include(i => i.Item)
                .Where(i => cart.Select(c => c.InventoryId).Contains(i.InventoryId))
                .ToDictionaryAsync(i => i.InventoryId, i => i.Item!.Name ?? "Unknown");

            // Return updated cart info
            return Json(new
            {
                success = true,
                cartCount = cart.Count,
                subtotal = subtotal.ToString("C"),
                discountApplied =  "None",
                items = cart.Select(c => new
                {
                    inventoryId = c.InventoryId,
                    itemName = names.TryGetValue(c.InventoryId, out var name) ? name : "Unknown",
                    quantity = c.Quantity,
                    unitPrice = c.UnitPrice.ToString("C"),
                    subtotal = (c.Quantity * c.UnitPrice).ToString("C")
                })
            });
        }
        // POST: Remove item from cart
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

        // POST: Clear the entire cart
        [HttpPost("ClearCart")]
        [ValidateAntiForgeryToken]
        public IActionResult ClearCart(int shopId)
        {
            ClearCartSession();
            TempData["Success"] = "Sale cart cleared.";
            return RedirectToAction(nameof(Index), new { shopId });
        }

        // POST: Checkout and create order
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

            // Find existing customer if ID is there
            if (customerId.HasValue)
            {
                customer = await _context.Customer
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.CustomerId == customerId.Value);
            }

            // If no customer found, create a guest customer
            if (customer == null)
            {
                customer = new Customer
                {
                    IsGuest = true,
                };
                _context.Customer.Add(customer);
                await _context.SaveChangesAsync();
            }

            // Calculate discount based on customer points for the registered customer
            decimal discountRate = 0m;
            int points = 0;

            if (customer.User != null && !customer.IsGuest && customer.IsEmailConfirmed)
            {
                points = customer.User.AccountPoints;
                discountRate = DiscountHelper.GetDiscountRate(points);
            }

            // Create the order
            var order = new Order
            {
                CustomerId = customer.CustomerId,
                ShopId = shop.ShopId,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Ordered
            };

            _context.Order.Add(order);
            await _context.SaveChangesAsync();

            // Add order items and update inventory
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
                var finalPrice = DiscountHelper.ApplyDiscount(inventory.SellPrice, points);
                var orderItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    InventoryId = inventory.InventoryId,
                    Quantity = item.Quantity,
                    UnitPrice = inventory.SellPrice, // Price before discount
                    UnitBuyPrice = inventory.Item.BuyPrice, 
                    DiscountPercent = discountRate // Appllies the overall customer discount to the order item
                };

                _context.OrderItem.Add(orderItem);

                // update inventory stock
                inventory.Quantity -= item.Quantity;
            }

            await _context.SaveChangesAsync();

            // Award points to the customer based on the total spent
            if (customer.User != null && !customer.IsGuest)
            {
                var totalSpent = cart.Sum(c => c.Quantity * c.UnitPrice);

                int earnedPoints = (int)Math.Floor(totalSpent * 10);

                customer.User.AccountPoints += earnedPoints;
                await _context.SaveChangesAsync();

                TempData["Success"] = (TempData["Success"] ?? "") + $" You earned {earnedPoints} points!";
            }

            ClearCartSession();

            TempData["Success"] = discountRate > 0
                ? $"Checkout complete with {discountRate:P0} discount applied. {(TempData["Success"] ?? "")}"
                : $"Checkout complete. {(TempData["Success"] ?? "")}";

            // Redirect to receipt
            return RedirectToAction(nameof(Receipt), new { shopId = shop.ShopId, orderId = order.OrderId });

        }
        // AJAX: Remove one quantity of an item from the cart
        [HttpPost("RemoveOne")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveOne(int shopId, int inventoryId)
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

            var names = await _context.Inventory
                .Include(i => i.Item)
                .Where(i => cart.Select(c => c.InventoryId).Contains(i.InventoryId))
                .ToDictionaryAsync(i => i.InventoryId, i => i.Item!.Name ?? "Unknown");

            var items = cart.Select(c => new
            {
                inventoryId = c.InventoryId,
                itemName = names.TryGetValue(c.InventoryId, out var name) ? name : "Unknown",
                quantity = c.Quantity,
                unitPrice = c.UnitPrice.ToString("C"),
                subtotal = (c.Quantity * c.UnitPrice).ToString("C")
            });

            // Return updated cart info
            return Json(new
            {
                success = true,
                cartCount = cart.Sum(c => c.Quantity),
                subtotal = subtotal.ToString("C"),
                items
            });
        }

        // AJAX: Search for customers by email or phone
        [HttpGet("SearchCustomer")]
        public async Task<IActionResult> SearchCustomer(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Json(new List<object>());

            query = query.Trim().ToLower();

            // Search for registered customers only
            var customers = await _context.Customer
                .Include(c => c.User)
                .Where(c =>
                    !c.IsGuest &&                           
                    c.IsEmailConfirmed &&                   
                    c.User != null && (
                        (c.User.Email != null && EF.Functions.Like(c.User.Email.ToLower(), $"%{query}%")) ||
                        (c.User.UserName != null && EF.Functions.Like(c.User.UserName.ToLower(), $"%{query}%")) ||
                        (c.User.PhoneNumber != null && EF.Functions.Like(c.User.PhoneNumber.ToLower(), $"%{query}%"))
                    )
                )
                .Take(10)
                .ToListAsync();

            // Select the specific customer data to return
            var result = customers.Select(c => new
            {
                c.CustomerId,
                Name = c.User.UserName,
                Email = c.User.Email,
                Phone = c.User.PhoneNumber,
                Points = c.User.AccountPoints,
                Status = GetStatusName(c.User.AccountPoints)
            });

            return Json(result);
        }

        // AJAX: Get customer discount details
        [HttpGet("GetCustomerDiscount/{customerId}")]
        public async Task<IActionResult> GetCustomerDiscount(int customerId)
        {
            var customer = await _context.Customer
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null || customer.User == null)
                return Json(new { success = false, message = "Customer not found." });

            int points = customer.User.AccountPoints;
            decimal discountRate = DiscountHelper.GetDiscountRate(points);
            string status = GetStatusName(points);

            return Json(new
            {
                success = true,
                customerId = customer.CustomerId,
                name = customer.User.UserName,
                email = customer.User.Email,
                phone = customer.User.PhoneNumber,
                points,
                status,
                discountRate = discountRate.ToString("P0")
            });
        }
        // Helper to get status name based on points
        private string GetStatusName(int points)
        {
            if (points >= StatusPoints.Platinum) return UserStatus.Platinum;
            if (points >= StatusPoints.Gold) return UserStatus.Gold;
            if (points >= StatusPoints.Silver) return UserStatus.Silver;
            if (points >= StatusPoints.Bronze) return UserStatus.Bronze;
            return UserStatus.Unranked;
        }

        // GET: Display receipt for an order
        [HttpGet("Receipt/{orderId}")]
        public async Task<IActionResult> Receipt(int shopId, int orderId)
        {
            var order = await _context.Order
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Inventory)
                        .ThenInclude(i => i.Item)
                .Include(o => o.Customer)
                    .ThenInclude(c => c.User)
                .Include(o => o.Shop)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.ShopId == shopId);

            if (order == null)
                return NotFound();
  
            var user = order.Customer?.User;

            // Default values for guest or unregistered customer
            int points = 0;
            decimal discountRate = 0m;
            string tier = UserStatus.Unranked;
            int pointsEarned = 0;

            var originalSubtotal = order.OrderItems.Sum(i => i.Quantity * i.UnitPrice);

            // Recalculate based on current user points if registered and confirmed
            if (user != null && order.Customer != null && !order.Customer.IsGuest)
            {
                points = user.AccountPoints;
                discountRate = DiscountHelper.GetDiscountRate(points);
                tier = GetStatusName(points);

                var totalDiscountAmount = originalSubtotal * discountRate;
                var finalTotalPaid = originalSubtotal - totalDiscountAmount;
                pointsEarned = (int)Math.Floor(finalTotalPaid * 10);
            }

            // Final calculations for display
            var totalDiscountAmountFinal = originalSubtotal * discountRate;
            var finalTotalPaidFinal = originalSubtotal - totalDiscountAmountFinal;
            var currentBalance = points + pointsEarned;

            // Pass data to the view
            ViewBag.CustomerName = user?.UserName ?? "Guest";
            ViewBag.Points = points;
            ViewBag.Status = tier;
            ViewBag.DiscountRate = discountRate;
            ViewBag.PointsEarned = pointsEarned;
            ViewBag.CurrentBalance = currentBalance;
            ViewBag.OriginalSubtotal = originalSubtotal;
            ViewBag.TotalDiscountAmount = totalDiscountAmountFinal;
            ViewBag.FinalTotalPaid = finalTotalPaidFinal;

            return View(order);
        }

    }
}


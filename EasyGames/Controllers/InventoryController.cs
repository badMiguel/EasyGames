using EasyGames.Data;
using EasyGames.Models;
using EasyGames.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Build.Framework;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EasyGames.Controllers
{
    [Route("Shop/{shopId}/Inventory")]
    [Authorize(Roles = UserRoles.Owner + "," + UserRoles.ShopProprietor)]
    public class InventoryController : Controller
    {
        private readonly EasyGamesContext _context;

        public InventoryController(EasyGamesContext context)
        {
            _context = context;
        }

        private bool IsOwnerOfShop(int? shopId)
        {
            if (shopId == null)
                return false;
            if (User.IsInRole(UserRoles.Owner))
                return true;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (_context.Shop.Find(shopId)?.OwnerId == userId)
            {
                return true;
            }

            return false;
        }

        private async Task<int> GetUnitsSoldByShop(int inventoryId)
        {
            return await _context
                .OrderItem.Where(oi => oi.InventoryId == inventoryId)
                .SumAsync(oi => oi.Quantity);
        }

        private async Task<decimal> GetItemRevenue(int inventoryId)
        {
            return await _context
                .OrderItem.Where(oi => oi.InventoryId == inventoryId)
                .SumAsync(oi => oi.Quantity * oi.UnitPrice);
        }

        private async Task<decimal> GetShopRevenue(int shopId)
        {
            return await _context
                .OrderItem.Where(oi => oi.Inventory.ShopId == shopId)
                .SumAsync(oi => oi.Quantity * oi.UnitPrice);
        }

        private async Task<decimal> GetItemProfit(int inventoryId)
        {
            return await _context
                .OrderItem.Where(oi => oi.InventoryId == inventoryId)
                .SumAsync(oi => oi.Quantity * (oi.UnitPrice - oi.UnitBuyPrice));
        }

        private async Task<decimal> GetShopProfit(int shopId)
        {
            return await _context
                .OrderItem.Where(oi => oi.Inventory.ShopId == shopId)
                .SumAsync(oi => oi.Quantity * (oi.UnitPrice - oi.UnitBuyPrice));
        }

        // GET: Inventory
        [HttpGet("")]
        public async Task<IActionResult> Index(int shopId, int? pageNumber, int? pageSize)
        {
            var shop = await _context.Shop.FindAsync(shopId);
            if (!IsOwnerOfShop(shop?.ShopId))
                return Forbid();

            var inventories = _context
                .Inventory.Include(i => i.Item)
                .Include(i => i.Shop)
                .OrderBy(i => i.Item.Name)
                .Where(i => i.ShopId == shopId)
                .AsNoTracking();

            var paginatedInventory = await Pagination<Inventory>.CreateAsync(inventories, pageNumber, pageSize);

            var inventoryDetails = new List<InventoryDetailViewModel>();
            foreach (var i in paginatedInventory)
            {
                var totalUnitsSold = await GetUnitsSoldByShop(i.InventoryId);
                var revenue = await GetItemRevenue(i.InventoryId);
                var profitGenerated = await GetItemProfit(i.InventoryId);
                inventoryDetails.Add(new InventoryDetailViewModel
                {
                    InventoryId = i.InventoryId,
                    ItemId = i.ItemId,
                    Item = i.Item,
                    SellPrice = i.SellPrice,
                    Quantity = i.Quantity,
                    Revenue = await GetItemRevenue(i.InventoryId),
                    TotalUnitsSold = await GetUnitsSoldByShop(i.InventoryId),
                    ProfitGenerated = await GetItemProfit(i.InventoryId),
                });
            }

            var inventoryIndex = new InventoryIndexViewModel
            {
                ShopId = shopId,
                ShopName = shop!.ShopName,
                TotalProfit = await GetShopProfit(shopId),
                TotalRevenue = await GetShopRevenue(shopId),
                InventoryItems = inventoryDetails,
                PageDetails = new PageDetails
                {
                    HasPreviousPage = paginatedInventory.HasPreviousPage,
                    HasNextPage = paginatedInventory.HasNextPage,
                    PageIndex = paginatedInventory.PageIndex,
                    PageSize = paginatedInventory.PageSize,
                }
            };

            return View(inventoryIndex);
        }

        // GET: Inventory/Details/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventory = await _context
                .Inventory.Include(i => i.Item)
                .Include(i => i.Shop)
                .FirstOrDefaultAsync(m => m.InventoryId == id);

            if (!IsOwnerOfShop(inventory?.ShopId))
                return Forbid();

            if (inventory == null)
            {
                return NotFound();
            }

            var inventoryDetails = new InventoryDetailViewModel
            {
                InventoryId = inventory.InventoryId,
                Item = inventory.Item,
                ItemId = inventory.ItemId,
                Shop = inventory.Shop,
                ShopId = inventory.ShopId,
                SellPrice = inventory.SellPrice,
                Quantity = inventory.Quantity,
                TotalUnitsSold = await GetUnitsSoldByShop(inventory.InventoryId),
                Revenue = await GetItemRevenue(inventory.InventoryId),
                ProfitGenerated = await GetItemProfit(inventory.InventoryId),
            };

            return View(inventoryDetails);
        }

        // GET: Inventory/Create
        // Authorise Owner only
        [HttpGet("Create")]
        [Authorize(Roles = UserRoles.Owner)]
        public IActionResult Create([FromRoute] int shopId)
        {
            if (!IsOwnerOfShop(shopId))
                return Forbid();

            ViewData["ShopId"] = shopId;

            ViewData["ItemIdList"] = new SelectList(_context.Item, "ItemId", "Name");
            ViewData["ShopIdList"] = new SelectList(_context.Shop, "ShopId", "ShopName");
            return View();
        }

        // POST: Inventory/Create
        // Authorise Owner only
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.Owner)]
        public async Task<IActionResult> Create(
            [Bind("InventoryId,ShopId,ItemId,SellPrice,Quantity")] Inventory inventory
        )
        {
            if (!IsOwnerOfShop(inventory.ShopId))
                return Forbid();

            if (ModelState.IsValid)
            {
                var inventoryOfItem = await _context
                    .Inventory.Include(i => i.Item)
                    .Include(i => i.Shop)
                    .FirstOrDefaultAsync(i =>
                        i.ItemId == inventory.ItemId && i.ShopId == inventory.ShopId
                    );

                if (inventoryOfItem != null)
                {
                    ViewData["ItemIdList"] = new SelectList(
                        _context.Item,
                        "ItemId",
                        "Name",
                        inventory.ItemId
                    );
                    ViewData["ShopIdList"] = new SelectList(
                        _context.Shop,
                        "ShopId",
                        "ShopName",
                        inventory.ShopId
                    );
                    ViewData["ShopId"] = inventory.ShopId;

                    ModelState.AddModelError(
                        "ItemId",
                        $"Sorry, shop '{inventoryOfItem.Shop.ShopName}' already have item '{inventoryOfItem.Item.Name}' in their inventory."
                    );
                    return View(inventory);
                }

                _context.Add(inventory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { shopId = inventory.ShopId });
            }
            ViewData["ShopId"] = inventory.ShopId;
            ViewData["ItemIdList"] = new SelectList(
                _context.Item,
                "ItemId",
                "Name",
                inventory.ItemId
            );
            ViewData["ShopIdList"] = new SelectList(
                _context.Shop,
                "ShopId",
                "ShopName",
                inventory.ShopId
            );
            return View(inventory);
        }

        // GET: Inventory/Edit/5
        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Include Item navigation property so shop proprietors can see the item name
            var inventory = await _context.Inventory
                .Include(i => i.Item)
                .FirstOrDefaultAsync(i => i.InventoryId == id);

            if (inventory == null)
            {
                return NotFound();
            }

            if (!IsOwnerOfShop(inventory.ShopId))
                return Forbid();

            ViewData["ItemIdList"] = new SelectList(
                _context.Item,
                "ItemId",
                "Name",
                inventory.ItemId
            );
            ViewData["ShopIdList"] = new SelectList(
                _context.Shop,
                "ShopId",
                "ShopName",
                inventory.ShopId
            );
            return View(inventory);
        }

        // POST: Inventory/Edit/5
        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("InventoryId,ShopId,ItemId,SellPrice,Quantity")] Inventory inventory
        )
        {
            if (id != inventory.InventoryId)
            {
                return NotFound();
            }

            if (!IsOwnerOfShop(inventory.ShopId))
                return Forbid();

            if (ModelState.IsValid)
            {
                var inventoryOfItem = await _context
                    .Inventory.Include(i => i.Item)
                    .Include(i => i.Shop)
                    .FirstOrDefaultAsync(i =>
                        i.ItemId == inventory.ItemId && i.ShopId == inventory.ShopId
                    );

                var oldInventory = await _context.Inventory.FindAsync(inventory.InventoryId);

                if (inventoryOfItem != null && oldInventory.ItemId != inventory.ItemId)
                {
                    ViewData["ItemIdList"] = new SelectList(
                        _context.Item,
                        "ItemId",
                        "Name",
                        inventory.ItemId
                    );
                    ViewData["ShopIdList"] = new SelectList(
                        _context.Shop,
                        "ShopId",
                        "ShopName",
                        inventory.ShopId
                    );
                    ViewData["ShopId"] = inventory.ShopId;

                    ModelState.AddModelError(
                        "ItemId",
                        $"Sorry, shop '{inventoryOfItem.Shop.ShopName}' already have item '{inventoryOfItem.Item.Name}' in their inventory."
                    );
                    return View(inventory);
                }
                try
                {
                    oldInventory.ItemId = inventory.ItemId;
                    oldInventory.ShopId = inventory.ShopId;

                    // Only Owner can edit quantity
                    // Shop proprietors can only edit sell price
                    if (User.IsInRole(UserRoles.Owner))
                    {
                        oldInventory.Quantity = inventory.Quantity;
                    }
                    // If shop proprietor, keep original quantity 

                    oldInventory.SellPrice = inventory.SellPrice;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InventoryExists(inventory.InventoryId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index), new { shopId = inventory.ShopId });
            }
            ViewData["ItemIdList"] = new SelectList(
                _context.Item,
                "ItemId",
                "Name",
                inventory.ItemId
            );
            ViewData["ShopIdList"] = new SelectList(
                _context.Shop,
                "ShopId",
                "ShopName",
                inventory.ShopId
            );
            return View(inventory);
        }

        // GET: Inventory/Delete/5
        // Authorise Owner only
        [HttpGet("Delete/{id:int}")]
        [Authorize(Roles = UserRoles.Owner)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var inventory = await _context
                .Inventory.Include(i => i.Item)
                .Include(i => i.Shop)
                .FirstOrDefaultAsync(m => m.InventoryId == id);
            if (inventory == null)
            {
                return NotFound();
            }

            if (!IsOwnerOfShop(inventory.ShopId))
                return Forbid();

            return View(inventory);
        }

        // POST: Inventory/Delete/5
        // Authorise Owner only
        [HttpPost("Delete/{id:int}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.Owner)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inventory = await _context.Inventory.FindAsync(id);
            if (!IsOwnerOfShop(inventory.ShopId))
                return Forbid();

            if (inventory != null)
            {
                _context.Inventory.Remove(inventory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { shopId = inventory.ShopId });
        }

        // GET: Shop/{shopId}/Inventory/Restock
        [HttpGet("Restock")]
        public async Task<IActionResult> Restock(int shopId)
        {
            // Authorization check
            if (!IsOwnerOfShop(shopId))
                return Forbid();

            // Get the physical shop
            var shop = await _context.Shop.FindAsync(shopId);
            if (shop == null)
                return NotFound();

            // Only allow restocking for physical shops
            if (shop.LocationType != LocationTypes.Physical)
            {
                TempData["Error"] = "Only physical shops can restock from the owner.";
                return RedirectToAction(nameof(Index), new { shopId });
            }

            // Get the online shop 
            var onlineShop = await _context.Shop
                .FirstOrDefaultAsync(s => s.LocationType == LocationTypes.Online);

            if (onlineShop == null)
            {
                TempData["Error"] = "Online shop not found. Cannot restock.";
                return RedirectToAction(nameof(Index), new { shopId });
            }

            // Get items available in online shop with stock > 0
            var onlineInventory = await _context.Inventory
                .Include(i => i.Item)
                .Where(i => i.ShopId == onlineShop.ShopId && i.Quantity > 0)
                .OrderBy(i => i.Item.Name)
                .ToListAsync();

            // Prepare data for the view
            ViewData["ShopId"] = shopId;
            ViewData["ShopName"] = shop.ShopName;
            ViewData["OnlineShopId"] = onlineShop.ShopId;

            // Create SelectList for dropdown
            ViewData["AvailableItems"] = new SelectList(
                onlineInventory.Select(i => new
                {
                    InventoryId = i.InventoryId,
                    DisplayText = $"{i.Item.Name} ({i.Quantity} available)"
                }),
                "InventoryId",
                "DisplayText"
            );

            return View(onlineInventory);
        }

        // POST: Shop/{shopId}/Inventory/Restock
        // Reference: Stock transfer logic implementation assisted by Claude AI (Anthropic)
        // Claude AI Prompt 001 start
        [HttpPost("Restock")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restock(int shopId, int onlineInventoryId, int quantity)
        {
            // Authorisation check
            if (!IsOwnerOfShop(shopId))
                return Forbid();

            // Validate input
            if (quantity <= 0)
            {
                TempData["Error"] = "Quantity must be greater than 0.";
                return RedirectToAction(nameof(Restock), new { shopId });
            }

            // Get the physical shop
            var physicalShop = await _context.Shop.FindAsync(shopId);
            if (physicalShop == null || physicalShop.LocationType != LocationTypes.Physical)
            {
                TempData["Error"] = "Invalid physical shop.";
                return RedirectToAction(nameof(Index), new { shopId });
            }

            // Get the online shop inventory item
            var onlineInventory = await _context.Inventory
                .Include(i => i.Item)
                .Include(i => i.Shop)
                .FirstOrDefaultAsync(i => i.InventoryId == onlineInventoryId);

            if (onlineInventory == null)
            {
                TempData["Error"] = "Item not found in online shop.";
                return RedirectToAction(nameof(Restock), new { shopId });
            }

            // Validate online shop has this item
            if (onlineInventory.Shop.LocationType != LocationTypes.Online)
            {
                TempData["Error"] = "Invalid online shop inventory.";
                return RedirectToAction(nameof(Restock), new { shopId });
            }

            // Validate sufficient quantity available
            if (onlineInventory.Quantity < quantity)
            {
                TempData["Error"] = $"Insufficient stock. Only {onlineInventory.Quantity} units of '{onlineInventory.Item.Name}' available in online shop.";
                return RedirectToAction(nameof(Restock), new { shopId });
            }

            // Check if physical shop already has this item
            var physicalInventory = await _context.Inventory
                .FirstOrDefaultAsync(i => i.ShopId == shopId && i.ItemId == onlineInventory.ItemId);

            if (physicalInventory != null)
            {
                // Item exists then add to existing quantity
                physicalInventory.Quantity += quantity;
            }
            else
            {
                // Item doesn't exist then create new inventory record
                // Inherit sell price from online shop
                physicalInventory = new Inventory
                {
                    ShopId = shopId,
                    ItemId = onlineInventory.ItemId,
                    SellPrice = onlineInventory.SellPrice, // Auto inherit price
                    Quantity = quantity
                };
                _context.Inventory.Add(physicalInventory);
            }

            // Decrease online shop inventory
            onlineInventory.Quantity -= quantity;

            // Save changes
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Successfully restocked {quantity} units of '{onlineInventory.Item.Name}' from online shop.";
            return RedirectToAction(nameof(Index), new { shopId });
        }
        // Claude AI Prompt 001 End

        private bool InventoryExists(int id)
        {
            return _context.Inventory.Any(e => e.InventoryId == id);
        }
    }
}
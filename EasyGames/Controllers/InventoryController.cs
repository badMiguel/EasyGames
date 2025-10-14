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
        [HttpGet("Create")]
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
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
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

            var inventory = await _context.Inventory.FindAsync(id);
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
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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
                    oldInventory.Quantity = inventory.Quantity;
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
        [HttpGet("Delete/{id:int}")]
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
        [HttpPost("Delete/{id:int}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
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

        private bool InventoryExists(int id)
        {
            return _context.Inventory.Any(e => e.InventoryId == id);
        }
    }
}

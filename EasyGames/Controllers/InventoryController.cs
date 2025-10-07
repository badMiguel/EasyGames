using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyGames.Data;
using EasyGames.Data;
using EasyGames.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EasyGames.Controllers
{
    [Authorize(Roles = UserRoles.Owner + "," + UserRoles.ShopProprietor)]
    public class InventoryController : Controller
    {
        private readonly EasyGamesContext _context;

        public InventoryController(EasyGamesContext context)
        {
            _context = context;
        }

        // GET: Inventory
        public async Task<IActionResult> Index()
        {
            var easyGamesContext = _context.Inventory.Include(i => i.Item).Include(i => i.Shop);
            return View(await easyGamesContext.ToListAsync());
        }

        // GET: Inventory/Details/5
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
            if (inventory == null)
            {
                return NotFound();
            }

            return View(inventory);
        }

        // GET: Inventory/Create
        public IActionResult Create()
        {
            ViewData["ItemId"] = new SelectList(_context.Item, "ItemId", "Name");
            ViewData["ShopId"] = new SelectList(_context.Shop, "ShopId", "ShopName");
            return View();
        }

        // POST: Inventory/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("InventoryId,ShopId,ItemId,Quantity")] Inventory inventory
        )
        {
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
                    ViewData["ItemId"] = new SelectList(
                        _context.Item,
                        "ItemId",
                        "Name",
                        inventory.ItemId
                    );
                    ViewData["ShopId"] = new SelectList(
                        _context.Shop,
                        "ShopId",
                        "ShopName",
                        inventory.ShopId
                    );

                    ModelState.AddModelError(
                        "ItemId",
                        $"Sorry, shop '{inventoryOfItem.Shop.ShopName}' already have item '{inventoryOfItem.Item.Name}' in their inventory."
                    );
                    return View(inventory);
                }

                _context.Add(inventory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ItemId"] = new SelectList(_context.Item, "ItemId", "Name", inventory.ItemId);
            ViewData["ShopId"] = new SelectList(
                _context.Shop,
                "ShopId",
                "ShopName",
                inventory.ShopId
            );
            return View(inventory);
        }

        // GET: Inventory/Edit/5
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
            ViewData["ItemId"] = new SelectList(_context.Item, "ItemId", "Name", inventory.ItemId);
            ViewData["ShopId"] = new SelectList(
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("InventoryId,ShopId,ItemId,Quantity")] Inventory inventory
        )
        {
            if (id != inventory.InventoryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(inventory);
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
                return RedirectToAction(nameof(Index));
            }
            ViewData["ItemId"] = new SelectList(_context.Item, "ItemId", "Name", inventory.ItemId);
            ViewData["ShopId"] = new SelectList(
                _context.Shop,
                "ShopId",
                "ShopName",
                inventory.ShopId
            );
            return View(inventory);
        }

        // GET: Inventory/Delete/5
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

            return View(inventory);
        }

        // POST: Inventory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inventory = await _context.Inventory.FindAsync(id);
            if (inventory != null)
            {
                _context.Inventory.Remove(inventory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool InventoryExists(int id)
        {
            return _context.Inventory.Any(e => e.InventoryId == id);
        }
    }
}

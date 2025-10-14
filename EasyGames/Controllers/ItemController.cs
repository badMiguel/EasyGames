using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyGames.Data;
using EasyGames.Models;
using EasyGames.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EasyGames.Controllers
{
    [Authorize(Roles = UserRoles.Owner)]
    public class ItemController : Controller
    {
        private readonly EasyGamesContext _context;

        public ItemController(EasyGamesContext context)
        {
            _context = context;
        }

        // GET: Item
        public async Task<IActionResult> Index(int? pageNumber, int? pageSize)
        {
            var paginatedItems = await Pagination<Item>.CreateAsync(
                _context.Item.AsNoTracking(),
                pageNumber,
                pageSize
            );
            ViewData["PageDetails"] = new PageDetails
            {
                PageSize = paginatedItems.PageSize,
                PageIndex = paginatedItems.PageIndex,
                HasNextPage = paginatedItems.HasNextPage,
                HasPreviousPage = paginatedItems.HasPreviousPage,
            };

            return View(paginatedItems);
        }

        // GET: Item/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Item.FirstOrDefaultAsync(m => m.ItemId == id);
            if (item == null)
            {
                return NotFound();
            }

            var unitsSold = await _context
                .OrderItem.Include(oi => oi.Inventory)
                .ThenInclude(i => i.Item)
                .Where(oi => oi.Inventory.ItemId == item.ItemId)
                .SumAsync(oi => oi.Quantity);

            var revenue = await _context
                .OrderItem.Include(oi => oi.Inventory)
                .ThenInclude(i => i.Item)
                .Where(oi => oi.Inventory.ItemId == item.ItemId)
                .SumAsync(oi => oi.Quantity * oi.UnitPrice);

            var profit = await _context
                .OrderItem.Include(oi => oi.Inventory)
                .ThenInclude(i => i.Item)
                .Where(oi => oi.Inventory.ItemId == item.ItemId)
                .SumAsync(oi => oi.Quantity * (oi.UnitPrice - oi.UnitBuyPrice));

            var itemDetailsOwner = new ItemDetailsOwnerViewModel
            {
                Item = item,
                ItemId = item.ItemId,
                Revenue = revenue,
                TotalUnitsSold = unitsSold,
                ProfitGenerated = profit,
            };

            return View(itemDetailsOwner);
        }

        // GET: Item/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Item/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ItemId,Name,BuyPrice,ProductionDate,Description")] Item item
        )
        {
            if (ModelState.IsValid)
            {
                _context.Add(item);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(item);
        }

        // GET: Item/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Item.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            return View(item);
        }

        // POST: Item/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("ItemId,Name,BuyPrice,ProductionDate,Description")] Item item
        )
        {
            if (id != item.ItemId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(item);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ItemExists(item.ItemId))
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
            return View(item);
        }

        // GET: Item/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var item = await _context.Item.FirstOrDefaultAsync(m => m.ItemId == id);
            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        // POST: Item/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Item.FindAsync(id);
            if (item != null)
            {
                _context.Item.Remove(item);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ItemExists(int id)
        {
            return _context.Item.Any(e => e.ItemId == id);
        }
    }
}

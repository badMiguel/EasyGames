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
    public class ItemCategoryController : Controller
    {
        private readonly EasyGamesContext _context;

        public ItemCategoryController(EasyGamesContext context)
        {
            _context = context;
        }

        // GET: ItemCategory
        public async Task<IActionResult> Index(int? pageNumber, int? pageSize)
        {
            var itemCategory = _context
                .ItemCategory.Include(i => i.Category)
                .Include(i => i.Item)
                .AsNoTracking();

            var paginatedItemCategory = await Pagination<ItemCategory>.CreateAsync(
                itemCategory,
                pageNumber,
                pageSize
            );

            ViewData["PageDetails"] = new PageDetails
            {
                PageSize = paginatedItemCategory.PageSize,
                PageIndex = paginatedItemCategory.PageIndex,
                HasNextPage = paginatedItemCategory.HasNextPage,
                HasPreviousPage = paginatedItemCategory.HasPreviousPage,
            };

            return View(paginatedItemCategory);
        }

        // GET: ItemCategory/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var itemCategory = await _context
                .ItemCategory.Include(i => i.Category)
                .Include(i => i.Item)
                .FirstOrDefaultAsync(m => m.ItemCategoryId == id);
            if (itemCategory == null)
            {
                return NotFound();
            }

            return View(itemCategory);
        }

        // GET: ItemCategory/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Category, "CategoryId", "Name");
            ViewData["ItemId"] = new SelectList(_context.Item, "ItemId", "Name");
            return View();
        }

        // POST: ItemCategory/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ItemCategoryId,ItemId,CategoryId")] ItemCategory itemCategory
        )
        {
            if (ModelState.IsValid)
            {
                _context.Add(itemCategory);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(
                _context.Category,
                "CategoryId",
                "Name",
                itemCategory.CategoryId
            );
            ViewData["ItemId"] = new SelectList(
                _context.Item,
                "ItemId",
                "Name",
                itemCategory.ItemId
            );
            return View(itemCategory);
        }

        // GET: ItemCategory/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var itemCategory = await _context.ItemCategory.FindAsync(id);
            if (itemCategory == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(
                _context.Category,
                "CategoryId",
                "Name",
                itemCategory.CategoryId
            );
            ViewData["ItemId"] = new SelectList(
                _context.Item,
                "ItemId",
                "Name",
                itemCategory.ItemId
            );
            return View(itemCategory);
        }

        // POST: ItemCategory/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("ItemCategoryId,ItemId,CategoryId")] ItemCategory itemCategory
        )
        {
            if (id != itemCategory.ItemCategoryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(itemCategory);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ItemCategoryExists(itemCategory.ItemCategoryId))
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
            ViewData["CategoryId"] = new SelectList(
                _context.Category,
                "CategoryId",
                "Name",
                itemCategory.CategoryId
            );
            ViewData["ItemId"] = new SelectList(
                _context.Item,
                "ItemId",
                "Name",
                itemCategory.ItemId
            );
            return View(itemCategory);
        }

        // GET: ItemCategory/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var itemCategory = await _context
                .ItemCategory.Include(i => i.Category)
                .Include(i => i.Item)
                .FirstOrDefaultAsync(m => m.ItemCategoryId == id);
            if (itemCategory == null)
            {
                return NotFound();
            }

            return View(itemCategory);
        }

        // POST: ItemCategory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var itemCategory = await _context.ItemCategory.FindAsync(id);
            if (itemCategory != null)
            {
                _context.ItemCategory.Remove(itemCategory);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ItemCategoryExists(int id)
        {
            return _context.ItemCategory.Any(e => e.ItemCategoryId == id);
        }
    }
}

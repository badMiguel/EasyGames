using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
    [Authorize(Roles = UserRoles.Owner + "," + UserRoles.ShopProprietor)]
    public class ShopController : Controller
    {
        private readonly EasyGamesContext _context;

        public ShopController(EasyGamesContext context)
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

        // GET: Shop
        public async Task<IActionResult> Index(int? pageNumber, int? pageSize)
        {
            var shop = _context.Shop.Include(s => s.Owner).AsNoTracking();
            var paginatedShop = await Pagination<Shop>.CreateAsync(shop, pageNumber, pageSize);
            ViewData["PageDetails"] = new PageDetails
            {
                PageSize = paginatedShop.PageSize,
                PageIndex = paginatedShop.PageIndex,
                HasNextPage = paginatedShop.HasNextPage,
                HasPreviousPage = paginatedShop.HasPreviousPage,
            };
            return View(paginatedShop);
        }

        // GET: Shop/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!IsOwnerOfShop(id))
                return Forbid();

            var shop = await _context
                .Shop.Include(s => s.Owner)
                .FirstOrDefaultAsync(m => m.ShopId == id);
            if (shop == null)
            {
                return NotFound();
            }

            return View(shop);
        }

        // GET: Shop/Create
        public IActionResult Create()
        {
            ViewData["LocationType"] = new SelectList(
                Enum.GetValues(typeof(LocationTypes)),
                LocationTypes.Physical
            );
            ViewData["OwnerId"] = new SelectList(_context.Users, "Id", "UserName");
            return View();
        }

        // POST: Shop/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ShopId,ShopName,ContactNumber,LocationType,Address,OwnerId")] Shop shop
        )
        {
            if (ModelState.IsValid)
            {
                _context.Add(shop);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["OwnerId"] = new SelectList(_context.Users, "Id", "Id", shop.OwnerId);
            return View(shop);
        }

        // GET: Shop/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!IsOwnerOfShop(id))
                return Forbid();

            var shop = await _context.Shop.FindAsync(id);
            if (shop == null)
            {
                return NotFound();
            }
            ViewData["LocationType"] = new SelectList(Enum.GetValues(typeof(LocationTypes)));
            ViewData["OwnerId"] = new SelectList(_context.Users, "Id", "UserName", shop.OwnerId);
            return View(shop);
        }

        // POST: Shop/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("ShopId,ShopName,ContactNumber,LocationType,Address,OwnerId")] Shop shop
        )
        {
            if (id != shop.ShopId)
            {
                return NotFound();
            }

            if (!IsOwnerOfShop(id))
                return Forbid();

            if (ModelState.IsValid)
            {
                // Get the original shop from database
                var originalShop = await _context.Shop.AsNoTracking().FirstOrDefaultAsync(s => s.ShopId == id);

                if (originalShop == null)
                {
                    return NotFound();
                }

                // Prevent changing frm Online to Physical
                if (originalShop.LocationType == LocationTypes.Online && shop.LocationType == LocationTypes.Physical)
                {
                    ModelState.AddModelError("LocationType",
                        "Cannot change the Online Shop to a Physical shop. The Online Shop is the main inventory source for all physical shops.");

                    ViewData["LocationType"] = new SelectList(Enum.GetValues(typeof(LocationTypes)), shop.LocationType);
                    ViewData["OwnerId"] = new SelectList(_context.Users, "Id", "UserName", shop.OwnerId);
                    return View(shop);
                }

                // Prevent changing to online if an online shop already exists
                if (originalShop.LocationType == LocationTypes.Physical && shop.LocationType == LocationTypes.Online)
                {
                    // Check if an online shop already exists
                    var existingOnlineShop = await _context.Shop
                        .AnyAsync(s => s.LocationType == LocationTypes.Online && s.ShopId != id);

                    if (existingOnlineShop)
                    {
                        ModelState.AddModelError("LocationType",
                            "Cannot change this shop to Online. This is because an online Shop already exists. There can only be one Online Shop in the system.");

                        ViewData["LocationType"] = new SelectList(Enum.GetValues(typeof(LocationTypes)), shop.LocationType);
                        ViewData["OwnerId"] = new SelectList(_context.Users, "Id", "UserName", shop.OwnerId);
                        return View(shop);
                    }
                }

                try
                {
                    _context.Update(shop);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShopExists(shop.ShopId))
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

            ViewData["LocationType"] = new SelectList(Enum.GetValues(typeof(LocationTypes)), shop.LocationType);
            ViewData["OwnerId"] = new SelectList(_context.Users, "Id", "UserName", shop.OwnerId);
            return View(shop);
        }

        // GET: Shop/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!IsOwnerOfShop(id))
                return Forbid();

            var shop = await _context
                .Shop.Include(s => s.Owner)
                .FirstOrDefaultAsync(m => m.ShopId == id);
            if (shop == null)
            {
                return NotFound();
            }

            return View(shop);
        }

        // POST: Shop/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsOwnerOfShop(id))
                return Forbid();

            var shop = await _context.Shop.FindAsync(id);
            if (shop != null)
            {
                _context.Shop.Remove(shop);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ShopExists(int id)
        {
            return _context.Shop.Any(e => e.ShopId == id);
        }
    }
}

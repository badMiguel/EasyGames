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
        [Authorize(Roles = UserRoles.Owner)]
        public async Task<IActionResult> Create()
        {
            ViewData["LocationType"] = new SelectList(
                Enum.GetValues(typeof(LocationTypes)),
                LocationTypes.Physical
            );

            // Get shop proprietors who don't already own a shop
            var usersWithShops = _context.Shop.Select(s => s.OwnerId).ToList();
            var allUsers = await _context.Users.ToListAsync();

            var availableProprietors = new List<object>();

            foreach (var user in allUsers)
            {
                if (usersWithShops.Contains(user.Id))
                    continue;

                var roles = await _context.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Join(_context.Roles,
                        ur => ur.RoleId,
                        r => r.Id,
                        (ur, r) => r.Name)
                    .ToListAsync();

                // only ShopProprietor role 
                if (roles.Contains(UserRoles.ShopProprietor))
                {
                    availableProprietors.Add(new { user.Id, user.UserName });
                }
            }

            ViewData["OwnerId"] = new SelectList(availableProprietors, "Id", "UserName");
            ViewData["HasAvailableUsers"] = availableProprietors.Any();

            return View();
        }

        // POST: Shop/Create
        // Ensure that only Owner can create shops
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.Owner)]
        public async Task<IActionResult> Create(
            [Bind("ShopId,ShopName,ContactNumber,LocationType,Address,OwnerId")] Shop shop
        )
        {
            if (ModelState.IsValid)
            {
                // Check if user already owns a shop
                var userAlreadyOwnsShop = await _context.Shop
                    .AnyAsync(s => s.OwnerId == shop.OwnerId);

                if (userAlreadyOwnsShop)
                {
                    ModelState.AddModelError("OwnerId",
                        "This user already owns a shop. Each user can only own one shop.");

                    await RebuildCreateViewData(shop);
                    return View(shop);
                }

                // Check if selected user has appropriate role
                var userRoles = await _context.UserRoles
                    .Where(ur => ur.UserId == shop.OwnerId)
                    .Join(_context.Roles,
                        ur => ur.RoleId,
                        r => r.Id,
                        (ur, r) => r.Name)
                    .ToListAsync();

                if (!userRoles.Contains(UserRoles.Owner) && !userRoles.Contains(UserRoles.ShopProprietor))
                {
                    ModelState.AddModelError("OwnerId",
                        "Only users with Owner or Shop Proprietor role can own shops.");

                    await RebuildCreateViewData(shop);
                    return View(shop);
                }

                // Prevent creating a second online shop
                if (shop.LocationType == LocationTypes.Online)
                {
                    var existingOnlineShop = await _context.Shop
                        .AnyAsync(s => s.LocationType == LocationTypes.Online);

                    if (existingOnlineShop)
                    {
                        ModelState.AddModelError("LocationType",
                            "An Online Shop already exists. Only one Online Shop is allowed in the system. Please select 'Physical' location type instead.");

                        await RebuildCreateViewData(shop);
                        return View(shop);
                    }
                }

                _context.Add(shop);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await RebuildCreateViewData(shop);
            return View(shop);
        }

        // Helper method to rebuild ViewData
        private async Task RebuildCreateViewData(Shop shop)
        {
            var usersWithShops = _context.Shop.Select(s => s.OwnerId).ToList();
            var allUsers = await _context.Users.ToListAsync();

            var availableProprietors = new List<object>();

            foreach (var user in allUsers)
            {
                if (usersWithShops.Contains(user.Id))
                    continue;

                var roles = await _context.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Join(_context.Roles,
                        ur => ur.RoleId,
                        r => r.Id,
                        (ur, r) => r.Name)
                    .ToListAsync();

                // Only ShopProprietor role
                if (roles.Contains(UserRoles.ShopProprietor))
                {
                    availableProprietors.Add(new { user.Id, user.UserName });
                }
            }

            ViewData["LocationType"] = new SelectList(
                Enum.GetValues(typeof(LocationTypes)),
                shop.LocationType
            );
            ViewData["OwnerId"] = new SelectList(availableProprietors, "Id", "UserName", shop.OwnerId);
            ViewData["HasAvailableUsers"] = availableProprietors.Any();
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

            var shop = await _context.Shop.Include(s => s.Owner).FirstOrDefaultAsync(s => s.ShopId == id);
            if (shop == null)
            {
                return NotFound();
            }

            ViewData["LocationType"] = new SelectList(Enum.GetValues(typeof(LocationTypes)), shop.LocationType);

            // only show ShopProprietor role 
            var allUsers = await _context.Users.ToListAsync();
            var shopProprietors = new List<object>();

            foreach (var user in allUsers)
            {
                var roles = await _context.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Join(_context.Roles,
                        ur => ur.RoleId,
                        r => r.Id,
                        (ur, r) => r.Name)
                    .ToListAsync();

                // only ShopProprietor role 
                if (roles.Contains(UserRoles.ShopProprietor))
                {
                    shopProprietors.Add(new { user.Id, user.UserName });
                }
            }

            ViewData["OwnerId"] = new SelectList(shopProprietors, "Id", "UserName", shop.OwnerId);
            return View(shop);
        }

        // POST: Shop/Edit/5
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
                var originalShop = await _context.Shop.AsNoTracking().FirstOrDefaultAsync(s => s.ShopId == id);

                if (originalShop == null)
                {
                    return NotFound();
                }

                // If user is Shop Proprietor, preserve original values for restricted fields
                if (User.IsInRole(UserRoles.ShopProprietor))
                {
                    shop.LocationType = originalShop.LocationType;
                    shop.Address = originalShop.Address;
                    shop.OwnerId = originalShop.OwnerId;
                    // Only ShopName and ContactNumber are updated
                }
                else if (User.IsInRole(UserRoles.Owner))
                {
                    // Owner validations for LocationType changes

                    // Prevent changing FROM Online to Physical
                    if (originalShop.LocationType == LocationTypes.Online && shop.LocationType == LocationTypes.Physical)
                    {
                        ModelState.AddModelError("LocationType",
                            "Cannot change the Online Shop to a Physical shop. The Online Shop is the main inventory source for all physical shops.");

                        ViewData["LocationType"] = new SelectList(Enum.GetValues(typeof(LocationTypes)), shop.LocationType);
                        ViewData["OwnerId"] = new SelectList(_context.Users, "Id", "UserName", shop.OwnerId);
                        return View(shop);
                    }

                    // Prevent changing to Online if an online shop already exists
                    if (originalShop.LocationType == LocationTypes.Physical && shop.LocationType == LocationTypes.Online)
                    {
                        var existingOnlineShop = await _context.Shop
                            .AnyAsync(s => s.LocationType == LocationTypes.Online && s.ShopId != id);

                        if (existingOnlineShop)
                        {
                            ModelState.AddModelError("LocationType",
                                "Cannot change this shop to Online. An Online Shop already exists. Only one Online Shop is allowed in the system.");

                            ViewData["LocationType"] = new SelectList(Enum.GetValues(typeof(LocationTypes)), shop.LocationType);
                            ViewData["OwnerId"] = new SelectList(_context.Users, "Id", "UserName", shop.OwnerId);
                            return View(shop);
                        }
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
        // Ensure that only Owner can delete shops
        [Authorize(Roles = UserRoles.Owner)]
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
        // Ensure that only owner can delete shops
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = UserRoles.Owner)]
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
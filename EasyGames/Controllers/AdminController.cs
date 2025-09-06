using System.Text.Encodings.Web;
using EasyGames.Data;
using EasyGames.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace EasyGames.Controllers;

// Used AI to assist
// [Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        var users = _userManager.Users.ToList();
        return View(users);
    }

    // GET: Admin/Create
    public IActionResult Create()
    {
        return View();
    }

    // // POST: Admin/Create
    // [HttpPost]
    // [ValidateAntiForgeryToken]
    // public async Task<IActionResult> Create([Bind("CategoryId,Name")] Category category)
    // {
    //     if (ModelState.IsValid)
    //     {
    //         _context.Add(category);
    //         await _context.SaveChangesAsync();
    //         return RedirectToAction(nameof(Index));
    //     }
    //     return View(category);
    // }

    // GET: User/Edit/<user_id>
    public async Task<IActionResult> Edit(string? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        return View(user);
    }

    // GET: User/Edit/<user_id>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string id,
        [Bind("Id,Email,UserName")] ApplicationUser user
    )
    {
        var userToEdit = await _userManager.FindByIdAsync(id);
        if (id != user.Id || userToEdit == null)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            userToEdit.Email = user.Email;
            userToEdit.UserName = user.UserName;
            var result = await _userManager.UpdateAsync(userToEdit);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(user);
    }
}

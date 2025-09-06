using System.Text.Encodings.Web;
using EasyGames.Data;
using EasyGames.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EasyGames.Controllers;

// Used AI to assist
// [Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
    )
    {
        _userManager = userManager;
        _roleManager = roleManager;
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

    // POST: Admin/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("UserName,Email,Password")] CreateUserModel userInput
    )
    {
        if (!ModelState.IsValid)
            return View();

        var newUser = new ApplicationUser
        {
            UserName = userInput.UserName,
            Email = userInput.Email,
        };
        var result = await _userManager.CreateAsync(newUser, userInput.Password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(newUser, userInput.Role);
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View();
    }

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

using EasyGames.Data;
using EasyGames.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EasyGames.Controllers;

[Authorize]
public class ReviewController : Controller
{
    private readonly EasyGamesContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReviewController(EasyGamesContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // USED AI TO HELP WITH CODE
    public async Task<IActionResult> Add(int ItemId, int StarRating, string? Comment)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        var review = new Review
        {
            ItemId = ItemId,
            UserId = user.Id,
            Comment = Comment,
            StarRating = StarRating,
            ReviewDate = DateTime.UtcNow,
        };

        _context.Review.Add(review);
        await _context.SaveChangesAsync();

        return RedirectToAction("ItemDetails", "Home", new { id = ItemId });
    }
}

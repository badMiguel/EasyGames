using System.Diagnostics;
using EasyGames.Data;
using EasyGames.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EasyGames.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly EasyGamesContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(
        ILogger<HomeController> logger,
        EasyGamesContext context,
        UserManager<ApplicationUser> userManager
    )
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        var itemCards = new Dictionary<string, List<HomeItemCards>>();
        var categories = _context.Category.ToList();
        if (categories != null)
        {
            foreach (var category in categories)
            {
                if (category.Name == null)
                    continue;

                GetTopItems(itemCards, category.Name);
            }
        }
        return View(itemCards);
    }

    // Helper method to get top 3 items from each categories to display on the
    // home page.
    private void GetTopItems(Dictionary<string, List<HomeItemCards>> itemCards, string category)
    {
        var getItems = _context
            .Category.Include(c => c.ItemCategories)
            .ThenInclude(ic => ic.Item)
            .FirstOrDefault(c => c.Name == category);

        if (getItems == null)
        {
            return;
        }
        var items = getItems.ItemCategories.Select(ic => ic.Item).Take(3).ToList();

        var itemList = new List<HomeItemCards>();

        foreach (var item in items)
        {
            if (item == null)
                continue;

            var rating = GetRating(item.ItemId);

            itemList.Add(
                new HomeItemCards
                {
                    ItemId = item.ItemId,
                    Name = item.Name,
                    Category = category,
                    Price = item.SellPrice,
                    Rating = rating.AverageRating,
                    RatingCount = rating.RatingCount,
                }
            );
        }

        itemCards[category] = itemList;
    }

    private (double AverageRating, int RatingCount) GetRating(int itemId)
    {
        var reviews = _context.Review.Where(r => r.ItemId == itemId).ToList();
        if (reviews.Count() <= 0)
        {
            return (0, 0);
        }
        int sumRating = 0;
        int rateCounter = 0;
        foreach (var review in reviews)
        {
            sumRating += review.StarRating;
            rateCounter++;
        }

        return ((double)sumRating / reviews.Count, rateCounter);
    }

    public IActionResult Category(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return RedirectToAction("Index");
        }

        var getItems = _context
            .Category.Include(c => c.ItemCategories)
            .ThenInclude(ic => ic.Item)
            .FirstOrDefault(c => c.Name == name);

        if (getItems == null)
        {
            return RedirectToAction("CategoryNotFound");
        }

        ViewData["Category"] = name;
        var items = getItems.ItemCategories.Select(ic => ic.Item).ToList();

        var itemList = new List<HomeItemCards>();

        foreach (var item in items)
        {
            if (item == null)
                continue;

            var rating = GetRating(item.ItemId);

            itemList.Add(
                new HomeItemCards
                {
                    ItemId = item.ItemId,
                    Name = item.Name,
                    Category = name,
                    Price = item.SellPrice,
                    Rating = rating.AverageRating,
                    RatingCount = rating.RatingCount,
                }
            );
        }

        return View(itemList);
    }

    public IActionResult CategoryNotFound(string name)
    {
        return View();
    }

    public async Task<IActionResult> ItemDetails(int id = -1)
    {
        if (id <= -1)
        {
            return NotFound();
        }

        var item = await _context.Item.FirstOrDefaultAsync(m => m.ItemId == id);
        if (item == null)
        {
            return NotFound();
        }
        var rating = GetRating(id);
        var reviews = await GetReviews(item.ItemId);

        var currentUser = await _userManager.GetUserAsync(User);
        if (reviews.Reviews != null && currentUser != null)
        {
            bool exists = reviews.Reviews.Any(r => r.UserId == currentUser.Id);
            if (exists)
            {
                ViewData["UserReviewed"] = true;
            }
        }

        var inventory = await _context
            .Inventory.Include(i => i.Shop)
            .Include(i => i.Item)
            .FirstOrDefaultAsync(i =>
                i.Shop!.LocationType == LocationTypes.Online && i.ItemId == item.ItemId
            );
        if (inventory == null)
        {
            return NotFound();
        }

        var details = new ItemDetails
        {
            ItemId = item.ItemId,
            Item = item,
            Inventory = inventory,
            Rating = rating.AverageRating,
            RatingCount = rating.RatingCount,
            Reviews = reviews.Reviews,
            Reviewers = reviews.Reviewers,
        };

        return View(details);
    }

    private async Task<(List<Review>? Reviews, Dictionary<string, string>? Reviewers)> GetReviews(
        int itemId
    )
    {
        var reviews = _context.Review.Where(r => r.ItemId == itemId).ToList();
        if (reviews == null)
        {
            return (null, null);
        }
        var reviewers = new Dictionary<string, string>();

        foreach (var review in reviews)
        {
            if (review.UserId == null)
            {
                continue;
            }
            var reviewer = await _userManager.FindByIdAsync(review.UserId);
            reviewers[review.UserId] = reviewer.UserName;
        }

        return (reviews, reviewers);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(
            new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier }
        );
    }
}

using System.Diagnostics;
using EasyGames.Data;
using EasyGames.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EasyGames.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly EasyGamesContext _context;

    public HomeController(ILogger<HomeController> logger, EasyGamesContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        var itemCards = new Dictionary<string, List<HomeItemCards>>();
        var categories = _context.Category.ToList();
        if (categories != null)
        {
            foreach (var category in categories)
            {
                if (category.Name == null) continue;

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

        if (getItems == null) return;
        var items = getItems
            .ItemCategories.Select(ic => ic.Item)
            .Take(3)
            .ToList();

        var itemList = new List<HomeItemCards>();

        foreach (var item in items)
        {
            if (item == null) continue;


            var rating = GetRating(item.ItemId);

            itemList.Add(
                new HomeItemCards
                {
                    Name = item.Name,
                    Category = category,
                    Price = item.Price,
                    Rating = rating.AverageRating,
                    RatingCount = rating.RatingCount,
                }
            );
        }

        itemCards[category] = itemList;
    }

    private (int AverageRating, int RatingCount) GetRating(int itemId)
    {
        var reviews = _context.Review.Where(r => r.ItemId == itemId).ToList();
        if (reviews == null) return (0, 0);
        int sumRating = 0;
        int rateCounter = 0;
        foreach (var review in reviews)
        {
            sumRating += review.StarRating;
            rateCounter++;
        }
        return (sumRating / reviews.Count, rateCounter);
    }

    public IActionResult Category(string name)
    {
        if (string.IsNullOrEmpty(name)) return RedirectToAction("Index");

        var getItems = _context
            .Category.Include(c => c.ItemCategories)
            .ThenInclude(ic => ic.Item)
            .FirstOrDefault(c => c.Name == name);

        if (getItems == null) return RedirectToAction("CategoryNotFound");

        ViewData["Category"] = name;
        var items = getItems
            .ItemCategories.Select(ic => ic.Item)
            .ToList();

        var itemList = new List<HomeItemCards>();

        foreach (var item in items)
        {
            if (item == null) continue;


            var rating = GetRating(item.ItemId);

            itemList.Add(
                new HomeItemCards
                {
                    Name = item.Name,
                    Category = name,
                    Price = item.Price,
                    Rating = rating.AverageRating,
                    RatingCount = rating.RatingCount,
                }
            );
        }

        return View(itemList);
    }

    public IActionResult CategoryNotFound(string name) {
        return View();
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

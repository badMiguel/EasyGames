using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc;


namespace EasyGames.Controllers;

public class ShopController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}

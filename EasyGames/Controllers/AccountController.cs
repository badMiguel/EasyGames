using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc;


namespace EasyGames.Controllers;

public class AccountController : Controller
{
    public IActionResult Transactions()
    {
        return View();
    }
}

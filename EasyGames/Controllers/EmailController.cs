using System.Text.Encodings.Web;
using EasyGames.Controllers;
using EasyGames.Data;
using EasyGames.Models;
using EasyGames.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EasyGames.Controllers;

[Authorize(Roles = UserRoles.Owner)]
public class EmailController : Controller
{
    private readonly EasyGamesContext _context;
    private readonly IEmailService _emailService;
    private readonly UserManager<ApplicationUser> _userManager;

    public EmailController(
        EasyGamesContext context,
        IEmailService emailService,
        UserManager<ApplicationUser> userManager
    )
    {
        _emailService = emailService;
        _context = context;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        var emailRequest = new EmailGroupRequest
        {
            StatusOptions = new SelectList(UserStatus.AllStatus),
        };

        return View(emailRequest);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(EmailGroupRequest emailRequest)
    {
        if (!ModelState.IsValid)
        {
            return View(emailRequest);
        }

        var req = new EmailGroupRequest
        {
            SelectedStatus = emailRequest.SelectedStatus,
            Subject = emailRequest.Subject,
            Body = emailRequest.Body,
            StatusOptions = new SelectList(UserStatus.AllStatus),
        };

        if (req == null || req.Subject == null || req.Body == null)
        {
            return BadRequest("Invalid Email Request");
        }

        var recipients = new List<string?>();
        if (req.SelectedStatus == null)
        {
            recipients = _userManager.Users.Select(s => s.Email).ToList();
        }
        else
        {
            recipients = _userManager
                .Users.AsEnumerable()
                .Where(u => UserStatusHelper.Get(u.AccountPoints) == req.SelectedStatus)
                .Select(s => s.Email)
                .ToList();
        }

        if (!recipients.Any())
        {
            ModelState.AddModelError(
                "SelectedStatus",
                $"There are currently no {req.SelectedStatus} users yet."
            );
            return View(req);
        }

        try
        {
            await _emailService.SendGroupEmailAsync(recipients, req.Subject, req.Body);
            TempData["Success"] = true;
            return View(
                new EmailGroupRequest { StatusOptions = new SelectList(UserStatus.AllStatus) }
            );
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Failed to send emails: {ex.Message}");
            return View(req);
        }
    }
}

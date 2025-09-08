// Used AI to help generate this code

using System.Security.Claims;
using EasyGames.Data;
using EasyGames.Models;
using Microsoft.AspNetCore.Mvc;

public class CartSummaryViewComponent : ViewComponent
{
    private readonly EasyGamesContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CartSummaryViewComponent(
        EasyGamesContext context,
        IHttpContextAccessor httpContextAccessor
    )
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    private int GetUserOrderId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var loggedInUserId = user?.FindFirstValue(ClaimTypes.NameIdentifier);

        Console.WriteLine(loggedInUserId);

        var order = _context.Order.FirstOrDefault(o =>
            o.Status == OrderStatus.InCart && o.UserId == loggedInUserId
        );

        if (order == null)
        {
            return -1;
        }
        return order.OrderId;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var orderId = GetUserOrderId();
        if (orderId <= -1)
        {
            return View(0);
        }

        var cartCount = _context.OrderItem.Count(oi => oi.OrderId == orderId);
        return View(cartCount);
    }
}

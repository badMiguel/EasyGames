// Used AI to help generate this code

using System.Security.Claims;
using EasyGames.Data;
using EasyGames.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    private async Task<Customer?> GetCustomer()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var loggedInUserId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (loggedInUserId == null)
        {
            var getCustomer = await _context.Customer.FirstOrDefaultAsync(c =>
                c.UserId == loggedInUserId
            );
            return getCustomer;
        }
        return null;
    }

    private async Task<int> GetUserOrderId()
    {
        var customer = await GetCustomer();
        if (customer == null)
        {
            return -1;
        }

        var order = _context.Order.FirstOrDefault(o =>
            o.Status == OrderStatus.InCart && o.CustomerId == customer.CustomerId
        );

        if (order == null)
        {
            return -1;
        }
        return order.OrderId;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var orderId = await GetUserOrderId();
        if (orderId <= -1)
        {
            return View(0);
        }

        var cartCount = _context.OrderItem.Count(oi => oi.OrderId == orderId);
        return View(cartCount);
    }
}

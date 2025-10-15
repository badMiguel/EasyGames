// Used AI to help generate this code

using System.Security.Claims;
using EasyGames.Data;
using EasyGames.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

public class CartSummaryViewComponent : ViewComponent
{
    private readonly EasyGamesContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private const string GuestCustomerSessionKey = "GuestCustomerId";

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
        if (loggedInUserId != null)
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
        // 1) Try logged-in customer first
        var customer = await GetCustomer();
        int? customerId = customer?.CustomerId;

        // 2) If not logged in, try guest id from Session
        if (customerId == null)
        {
            var sess = _httpContextAccessor.HttpContext?.Session;
            customerId = sess?.GetInt32(GuestCustomerSessionKey);
        }

        if (customerId == null)
            return -1;

        var order = await _context.Order
            .FirstOrDefaultAsync(o => o.Status == OrderStatus.InCart
                                   && o.CustomerId == customerId.Value);

        return order?.OrderId ?? -1;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var orderId = await GetUserOrderId();
        if (orderId <= -1)
        {
            return View(0);
        }

        var cartCount = await _context.OrderItem.CountAsync(oi => oi.OrderId == orderId);
        return View(cartCount);
    }
}

namespace EasyGames.Models;

public class AccountStatusViewModel
{
    public int Points { get; set; }
    public string Tier { get; set; } = string.Empty;
    public List<Order>? RecentOrders { get; set; }
}

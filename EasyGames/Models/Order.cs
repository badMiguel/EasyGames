namespace EasyGames.Models;

public enum OrderStatus
{
    InCart = 0,
    Ordered = 1,
    // Cancelled = 2,
}

public class Order
{
    public int OrderId { get; set; }

    // Foreign key User (ASP.Net Core Identity)
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public int ShopId { get; set; }
    public Shop? Shop { get; set; }

    public DateTime? OrderDate { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.InCart;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

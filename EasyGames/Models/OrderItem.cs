namespace EasyGames.Models;

public class OrderItem
{
    public int OrderItemId { get; set; }

    // Foreign key Order
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    // Foreign key Item
    public int ItemId { get; set; }
    public Item? Item { get; set; }

    public int Quantity { get; set; }

    // Total Price wont be stored, just computed based on quantity
    public decimal UnitPrice { get; set; }
}

namespace EasyGames.Models;

public class Inventory
{
    public int InventoryId { get; set; }

    public int LocationId { get; set; }
    public InventoryLocation? Location { get; set; }

    public int ItemId { get; set; }
    public Item? Item { get; set; }

    public int Quantity { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

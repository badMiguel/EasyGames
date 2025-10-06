namespace EasyGames.Models;

public enum LocationTypes
{
    Online = 1,
    Physical = 2,
}

public class InventoryLocation
{
    public int InventoryLocationId { get; set; }
    public LocationTypes LocationType { get; set; } = LocationTypes.Physical;

    // Physical stores should have address, make sure to add validation
    public string? Address { get; set; }

    // Foreign key
    public int ShopId { get; set; }
    public Shop? Shop { get; set; }

    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}

using System.ComponentModel.DataAnnotations;

namespace EasyGames.Models;

public class Shop
{
    public int ShopId { get; set; }

    [Required]
    public string? ShopName { get; set; }

    [Phone]
    [Required]
    [MaxLength(20)]
    public string? ContactNumber { get; set; }

    // Foreign key User (ASP.Net Core Identity)
    public string? OwnerId { get; set; }
    public ApplicationUser? Owner { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public InventoryLocation? Location { get; set; }
}

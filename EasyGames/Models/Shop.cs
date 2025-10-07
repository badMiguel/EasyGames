using System.ComponentModel.DataAnnotations;

namespace EasyGames.Models;

public enum LocationTypes
{
    Online = 1,
    Physical = 2,
}

public class Shop
{
    public int ShopId { get; set; }

    [Required]
    [Display(Name = "Shop Name")]
    public string? ShopName { get; set; }

    [Phone]
    [Required]
    [MaxLength(20)]
    [Display(Name = "Contact Number")]
    public string? ContactNumber { get; set; }

    [Display(Name = "Location Type")]
    public LocationTypes LocationType { get; set; } = LocationTypes.Physical;

    // Physical stores should have address, make sure to add validation
    public string? Address { get; set; }

    // Foreign key User (ASP.Net Core Identity)
    [Display(Name = "Owner ID")]
    public string? OwnerId { get; set; }
    public ApplicationUser? Owner { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}

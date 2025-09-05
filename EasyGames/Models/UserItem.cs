using System.ComponentModel.DataAnnotations;

namespace EasyGames.Models;

public class UserItem
{
    public int UserItemId { get; set; }

    // Foreign key User (ASP.Net Core Identity)
    [Required]
    public string? UserId { get; set; }

    [Required]
    public ApplicationUser? User { get; set; }

    // Foreign key Item
    public int ItemId { get; set; }

    [Required]
    public Item? Item { get; set; }

    public int Quantity { get; set; }

    [DataType(DataType.Currency)]
    public decimal Price { get; set; }
}

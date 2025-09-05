using System.ComponentModel.DataAnnotations;

namespace EasyGames.Models;

public class ItemCategory
{
    public int ItemCategoryId { get; set; }
    public int ItemId { get; set; }

    [Required]
    public Item? Item { get; set; }

    public int CategoryId { get; set; }

    [Required]
    public Category? Category { get; set; }
}

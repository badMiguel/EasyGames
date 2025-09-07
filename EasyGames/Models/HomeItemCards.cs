using System.ComponentModel.DataAnnotations;

namespace EasyGames.Models;

public class HomeItemCards
{
    [Required]
    public string? Name { get; set; }

    [Required]
    public string? Category { get; set; }

    [Required]
    public decimal Rating { get; set; } = 0;

    [Required]
    public int RatingCount { get; set; } = 0;

    [Required]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }
}

namespace EasyGames.Models;

public class HomeItemCards
{
    public int ItemId { get; set; }
    public string? Name { get; set; }
    public string? Category { get; set; }
    public decimal Rating { get; set; } = 0;
    public int RatingCount { get; set; } = 0;
    public decimal Price { get; set; }
}

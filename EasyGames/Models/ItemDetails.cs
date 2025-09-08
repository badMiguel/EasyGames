namespace EasyGames.Models;

public class ItemDetails
{
    public int ItemId { get; set; }
    public Item? Item { get; set; }

    public List<Review>? Reviews { get; set; }
    public Dictionary<string, string>? Reviewers { get; set; }
    public int Rating { get; set; } = 0;
    public int RatingCount { get; set; } = 0;
}

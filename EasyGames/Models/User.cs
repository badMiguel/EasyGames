using Microsoft.AspNetCore.Identity;

namespace EasyGames.Models;

public class ApplicationUser : IdentityUser
{
    public int AccountPoints { get; set; }

    // Navigation to Shop Models (not an FK)
    public Shop? Shop { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    public Customer? Customer { get; set; }

    public const int BronzePoints = 500;
    public const int SilverPoints = 1_000;
    public const int GoldPoints = 5_000;
    public const int PlatinumPoints = 10_000;

    public string AccountStatus
    {
        get
        {
            if (AccountPoints >= PlatinumPoints)
                return "Platinum";
            if (AccountPoints >= GoldPoints)
                return "Gold";
            if (AccountPoints >= SilverPoints)
                return "Silver";
            return "Bronze";
        }
    }
}

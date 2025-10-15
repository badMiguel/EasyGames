using Microsoft.AspNetCore.Identity;

namespace EasyGames.Models;

public static class UserRoles
{
    public const string Owner = "Owner";
    public const string ShopProprietor = "ShopProprietor";
    public const string Customer = "Customer";

    public static readonly string[] AllRoles = { Owner, ShopProprietor, Customer };
}

public static class UserStatus
{
    public const string Platinum = "Platinum";
    public const string Gold = "Gold";
    public const string Silver = "Silver";
    public const string Bronze = "Bronze";
    public const string Unranked = "Unranked";

    public static readonly string[] AllStatus = { Platinum, Gold, Silver, Bronze, Unranked };
}

public static class StatusPoints
{
    public const int Bronze = 1;     // 1+ points
    public const int Silver = 50;    // 50+
    public const int Gold = 150;     // 150+
    public const int Platinum = 300; // 300+

    public static readonly int[] AllPoints = { Bronze, Silver, Gold, Platinum };
}

public class ApplicationUser : IdentityUser
{
    public int AccountPoints { get; set; }

    // Navigation to Shop Models (not an FK)
    public Shop? Shop { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    public Customer? Customer { get; set; }
}

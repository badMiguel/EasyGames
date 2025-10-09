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

    public static readonly string[] AllStatus = { Platinum, Gold, Silver, Bronze };
}

public static class StatusPoints
{
    public const int Bronze = 500;
    public const int Silver = 1_000;
    public const int Gold = 5_000;
    public const int Platinum = 10_000;

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

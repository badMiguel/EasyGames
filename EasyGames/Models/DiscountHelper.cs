namespace EasyGames.Models;

public static class DiscountHelper
{
    public static decimal GetDiscountRate(int points)
    {
        if (points >= StatusPoints.Platinum)
            return 0.15m;
        if (points >= StatusPoints.Gold)
            return 0.10m;
        if (points >= StatusPoints.Silver)
            return 0.05m;
        return 0m;
    }

    public static decimal ApplyDiscount(decimal originalPrice, int points)
    {
        var rate = GetDiscountRate(points);
        return originalPrice * (1 - rate);
    }
}

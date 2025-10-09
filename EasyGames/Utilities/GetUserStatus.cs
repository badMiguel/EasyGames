using EasyGames.Models;

namespace EasyGames.Utilities;

public static class UserStatusHelper
{
    public static string Get(int accountPoints)
    {
        if (accountPoints >= StatusPoints.Platinum)
            return UserStatus.Platinum;
        if (accountPoints >= StatusPoints.Gold)
            return UserStatus.Gold;
        if (accountPoints >= StatusPoints.Silver)
            return UserStatus.Silver;
        if (accountPoints >= StatusPoints.Bronze)
            return UserStatus.Bronze;
        return UserStatus.Unranked;
    }
}

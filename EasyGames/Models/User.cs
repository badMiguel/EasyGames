using Microsoft.AspNetCore.Identity;

namespace EasyGames.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<UserItem> UserItems {get; set;} = new List<UserItem>();
    public ICollection<Review> Reviews {get; set;} = new List<Review>();
}

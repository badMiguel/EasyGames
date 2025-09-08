using Microsoft.AspNetCore.Identity;

namespace EasyGames.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<Order> Orders {get; set;} = new List<Order>();
    public ICollection<Review> Reviews {get; set;} = new List<Review>();
}

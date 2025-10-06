using System.ComponentModel.DataAnnotations;

namespace EasyGames.Models;

public class Customer
{
    public int CustomerId { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public bool IsGuest { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

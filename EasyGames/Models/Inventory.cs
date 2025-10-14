using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EasyGames.Models;

[Index(nameof(ShopId), nameof(ItemId), IsUnique = true)]
public class Inventory
{
    public int InventoryId { get; set; }

    public int ShopId { get; set; }
    public Shop? Shop { get; set; }

    public int ItemId { get; set; }
    public Item? Item { get; set; }

    [DataType(DataType.Currency)]
    [Display(Name = "Sell Price")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal SellPrice { get; set; }

    [Display(Name = "Stock")]
    public int Quantity { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

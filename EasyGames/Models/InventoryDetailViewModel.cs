using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyGames.Models;

public class InventoryDetailViewModel
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

    [Display(Name = "Units Sold by Shop")]
    public int TotalUnitsSold { get; set; }

    [DataType(DataType.Currency)]
    [Display(Name = "Profit Generated")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal ProfitGenerated { get; set; }
    public int Quantity { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

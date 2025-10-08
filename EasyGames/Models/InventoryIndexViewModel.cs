using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyGames.Models;

public class InventoryIndexViewModel
{
    [DataType(DataType.Currency)]
    [Display(Name = "Total Profit")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalProfit { get; set; }

    [DataType(DataType.Currency)]
    [Display(Name = "Total Revenue")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalRevenue { get; set; }
    public int ShopId { get; set; }
    public string? ShopName { get; set; }

    public IEnumerable<InventoryDetailViewModel> InventoryItems { get; set; } =
        Enumerable.Empty<InventoryDetailViewModel>();
}

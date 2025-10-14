using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyGames.Models;

public class ItemDetailsOwnerViewModel
{
    public int ItemId { get; set; }
    public Item? Item { get; set; }

    [DataType(DataType.Currency)]
    [Display(Name = "Sell Price")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal SellPrice { get; set; }

    [Display(Name = "Units Sold by Shop")]
    public int TotalUnitsSold { get; set; }

    [DataType(DataType.Currency)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Revenue { get; set; }

    [DataType(DataType.Currency)]
    [Display(Name = "Profit Generated")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal ProfitGenerated { get; set; }
}

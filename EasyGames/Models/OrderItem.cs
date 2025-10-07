using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyGames.Models;

public class OrderItem
{
    public int OrderItemId { get; set; }

    // Foreign key Order
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    // Foreign key Item
    public int InventoryId { get; set; }
    public Inventory? Inventory { get; set; }

    public int Quantity { get; set; }

    // Total Price wont be stored, just computed based on quantity
    [Range(1, 100)]
    [DataType(DataType.Currency)]
    [Display(Name = "Sell Price")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    // Total BuyPrice wont be stored, just computed based on quantity
    [Range(1, 100)]
    [DataType(DataType.Currency)]
    [Display(Name = "Buy Price")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitBuyPrice { get; set; }

    // Discount stored as decimal, to be converted to percent
    // Default no discount was used
    [Display(Name = "Discount")]
    [Column(TypeName = "decimal(5,4)")]
    public decimal DiscountPercent { get; set; } = 0.0000M;
}

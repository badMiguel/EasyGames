using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyGames.Models;

public class UserTransactionViewModel
{
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    [DataType(DataType.Currency)]
    [Display(Name = "Total Cost")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCost { get; set; }

    public IEnumerable<OrderItem> OrderItems { get; set; } = Enumerable.Empty<OrderItem>();
}

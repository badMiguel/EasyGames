using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyGames.Models;

public class UserSalesViewModel
{
    [DataType(DataType.Currency)]
    [Display(Name = "Total Revenue")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalRevenue { get; set; }

    [DataType(DataType.Currency)]
    [Display(Name = "Total Profit")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalProfit { get; set; }

    public IEnumerable<UserTransactionViewModel> Transactions { get; set; } =
        Enumerable.Empty<UserTransactionViewModel>();

    public PageDetails? PageDetails { get; set; }
}

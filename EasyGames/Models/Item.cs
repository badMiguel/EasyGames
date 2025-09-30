using System.ComponentModel.DataAnnotations;

namespace EasyGames.Models;
//agnes test
public class Item
{
    public int ItemId { get; set; }

    [StringLength(60, MinimumLength = 3)]
    [Required]
    public string? Name { get; set; }

    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [Display(Name = "Production Date")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime ProductionDate { get; set; }
    public string? Description { get; set; }
    public int StockAmount { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<ItemCategory> ItemCategorys { get; set; } = new List<ItemCategory>();
}

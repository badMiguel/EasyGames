using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EasyGames.Models;

public class Item
{
    public int ItemId { get; set; }

    [StringLength(60, MinimumLength = 3)]
    [Required]
    public string? Name { get; set; }

    [Range(1, 100)]
    [DataType(DataType.Currency)]
    [Display(Name = "Buy Price")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal BuyPrice { get; set; }

    [Display(Name = "Production Date")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime ProductionDate { get; set; }
    public string? Description { get; set; }

    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<ItemCategory> ItemCategorys { get; set; } = new List<ItemCategory>();
}

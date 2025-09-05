using System.ComponentModel.DataAnnotations;

namespace EasyGames.Models;

public class Category
{
    public int CategoryId {get;set;}

    [Required]
    public string? Name {get;set;}

    public ICollection<ItemCategory> ItemCategories{get;set;} = new List<ItemCategory>();
}

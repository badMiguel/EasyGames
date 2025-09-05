using System.ComponentModel.DataAnnotations;

namespace EasyGames.Models;

public class Review
{
    public int ReviewId { get; set; }

    [Required]
    public string? UserId { get; set; }

    [Required]
    public ApplicationUser? User { get; set; }

    public int ItemId { get; set; }

    [Required]
    public Item? Item { get; set; }

    // Optional to comment
    public string? Comment { get; set; }
    public int StarRating { get; set; }

    [Display(Name = "ReviewDate")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime ReviewDate { get; set; }
}

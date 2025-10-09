using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EasyGames.Models;

public class EmailGroupRequest
{
    [Display(Name = "Select User Group")]
    public string? SelectedStatus { get; set; }
    public IEnumerable<SelectListItem> StatusOptions { get; set; } =
        Enumerable.Empty<SelectListItem>();

    [Required, MaxLength(200)]
    public string? Subject { get; set; }

    [Required]
    [DataType(DataType.MultilineText)]
    public string? Body { get; set; }
}

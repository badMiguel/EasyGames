namespace EasyGames.Models;

public class PageDetails
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }

    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}

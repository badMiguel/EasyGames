using Microsoft.EntityFrameworkCore;

namespace EasyGames.Utilities;

public class Pagination<T> : List<T>
{
    public int PageIndex { get; private set; }
    public int TotalPages { get; private set; }
    public int PageSize { get; private set; }

    public Pagination(List<T> items, int count, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        PageSize = pageSize;

        this.AddRange(items);
    }

    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public static async Task<Pagination<T>> CreateAsync(
        IQueryable<T> source,
        int? pageIndex = null,
        int? pageSize = null
    )
    {
        var count = await source.CountAsync();

        int index = pageIndex ?? 1;
        int size = pageSize ?? (count < 5 ? 5 : 10);

        var items = await source.Skip((index - 1) * size).Take(size).ToListAsync();
        return new Pagination<T>(items, count, index, size);
    }
}

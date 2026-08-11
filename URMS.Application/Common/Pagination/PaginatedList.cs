using Microsoft.EntityFrameworkCore;

namespace URMS.Application.Common.Pagination;

public class PaginatedList<T>
{
    public List<T> Items { get; private set; }
    public int PageNumber { get; private set; }
    public int TotalPages { get; private set; }
    public int TotalCount { get; private set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PaginatedList(List<T> items, int? pageNumber, int count, int? pageSize)
    {
        Items = items;
        PageNumber = pageNumber.HasValue && pageNumber.Value > 0 ? pageNumber.Value : 1;
        TotalCount = count;
        TotalPages = (pageSize.HasValue && pageSize.Value > 0) 
            ? (int)Math.Ceiling(count / (double)pageSize.Value) 
            : 1;
    }

    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int? pageNumber, int? pageSize, CancellationToken cancellationToken = default)
    {
        var count = await source.CountAsync(cancellationToken);

        if (!pageNumber.HasValue || !pageSize.HasValue || pageSize <= 0)
        {
            var allItems = await source.ToListAsync(cancellationToken);
            return new PaginatedList<T>(allItems, 1, count, count > 0 ? count : 1);
        }

        var pNum = pageNumber.Value < 1 ? 1 : pageNumber.Value;
        var pSize = pageSize.Value;

        var items = await source
            .Skip((pNum - 1) * pSize)
            .Take(pSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<T>(items, pNum, count, pSize);
    }

    public static PaginatedList<T> Create(List<T> sourceItems, int? pageNumber, int? pageSize)
    {
        var count = sourceItems.Count;

        if (!pageNumber.HasValue || !pageSize.HasValue || pageSize <= 0)
        {
            return new PaginatedList<T>(sourceItems, 1, count, count > 0 ? count : 1);
        }

        var pNum = pageNumber.Value < 1 ? 1 : pageNumber.Value;
        var pSize = pageSize.Value;

        var items = sourceItems
            .Skip((pNum - 1) * pSize)
            .Take(pSize)
            .ToList();

        return new PaginatedList<T>(items, pNum, count, pSize);
    }
}

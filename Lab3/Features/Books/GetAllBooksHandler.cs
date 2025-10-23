using Lab3.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lab3.Features.Books;

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems, int TotalPages);

public class GetAllBooksHandler(BookManagementContext context)
{
    private readonly BookManagementContext _context = context;

    public async Task<IResult> Handle(GetAllBooksRequest request)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var size = request.PageSize <= 0 ? 10 : Math.Min(request.PageSize, 100);

        var query = _context.Books.AsNoTracking();

        // FILTER: by author (case-insensitive, contains)
        if (!string.IsNullOrWhiteSpace(request.Author))
        {
            var author = request.Author.Trim().ToLower();
            query = query.Where(b => b.Author.ToLower().Contains(author));
        }

        // SORT: mapare strictă (whitelist) pentru a evita “injection” de nume arbitrare
        var sortBy = (request.SortBy ?? "id").ToLowerInvariant();
        var desc = string.Equals(request.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

        query = sortBy switch
        {
            "title" => desc ? query.OrderByDescending(b => b.Title) : query.OrderBy(b => b.Title),
            "year"  => desc ? query.OrderByDescending(b => b.Year)  : query.OrderBy(b => b.Year),
            _       => desc ? query.OrderByDescending(b => b.Id)    : query.OrderBy(b => b.Id) // default: Id
        };

        var total = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        var result = new PagedResult<Book>(
            Items: items,
            Page: page,
            PageSize: size,
            TotalItems: total,
            TotalPages: (int)Math.Ceiling(total / (double)size)
        );

        return Results.Ok(result);
    }
}
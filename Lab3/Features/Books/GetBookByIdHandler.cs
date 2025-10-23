using Lab3.Common.Exceptions;
using Lab3.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lab3.Features.Books;

public class GetBookByIdHandler(BookManagementContext context)
{
    private readonly BookManagementContext _context = context;

    public async Task<IResult> Handle(GetBookByIdRequest request)
    {
        var book = await _context.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == request.Id);
        if (book is null) throw new NotFoundException($"Book {request.Id} not found.");
        return Results.Ok(book);
    }
}
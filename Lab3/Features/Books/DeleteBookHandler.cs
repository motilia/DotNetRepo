using Lab3.Common.Exceptions;
using Lab3.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lab3.Features.Books;

public class DeleteBookHandler(BookManagementContext context)
{
    private readonly BookManagementContext _context = context;

    public async Task<IResult> Handle(DeleteBookRequest request)
    {
        var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == request.Id);
        if (book is null) throw new NotFoundException($"Book {request.Id} not found.");

        _context.Books.Remove(book);
        await _context.SaveChangesAsync();
        return Results.NoContent();
    }
}
using Lab3.Persistence;
using Lab3.Validators;
using Microsoft.EntityFrameworkCore;

namespace Lab3.Features.Books;

public class UpdateBookHandler(BookManagementContext context)
{
    private readonly BookManagementContext _context = context;

    public async Task<IResult> Handle(UpdateBookRequest request)
    {
        var validator = new UpdateBookValidator();
        var vr = await validator.ValidateAsync(request);
        if (!vr.IsValid) return Results.BadRequest(vr.Errors);

        var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == request.Id);
        if (book is null) return Results.NotFound();

        book.Title = request.Title;
        book.Author = request.Author;
        book.Year = request.Year;

        await _context.SaveChangesAsync();
        return Results.Ok(book);
    }
}
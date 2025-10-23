using Lab3.Persistence;
using Lab3.Validators;

namespace Lab3.Features.Books;

public class CreateBookHandler(BookManagementContext context)
{
    private readonly BookManagementContext _context = context;

    public async Task<IResult> Handle(CreateBookRequest request)
    {
        var validator = new CreateBookValidator();
        var vr = await validator.ValidateAsync(request);
        if (!vr.IsValid) return Results.BadRequest(vr.Errors);

        var book = new Book { Title = request.Title, Author = request.Author, Year = request.Year };
        _context.Books.Add(book);
        await _context.SaveChangesAsync();
        return Results.Created($"/books/{book.Id}", book);
    }
}
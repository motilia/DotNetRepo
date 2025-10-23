using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Lab3.Features.Books;
using Lab3.Persistence;
using Lab3.Validators;
using Lab3.Common.Middleware; 

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<BookManagementContext>(opt =>
    opt.UseSqlite("Data Source=books.db"));

builder.Services.AddScoped<CreateBookHandler>();
builder.Services.AddScoped<UpdateBookHandler>();
builder.Services.AddScoped<GetBookByIdHandler>();
builder.Services.AddScoped<GetAllBooksHandler>();
builder.Services.AddScoped<DeleteBookHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateBookValidator>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookManagementContext>();
    db.Database.EnsureCreated(); 
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.UseHttpsRedirection();


app.UseMiddleware<ErrorHandlingMiddleware>();


app.MapPost("/books", async (CreateBookRequest req, CreateBookHandler handler)
    => await handler.Handle(req));

app.MapGet("/books", async (
        int? page,
        int? pageSize,
        string? author,
        string? sortBy,
        string? sortDir,
        GetAllBooksHandler handler) =>
    await handler.Handle(new GetAllBooksRequest(
        page ?? 1,
        pageSize ?? 10,
        author,
        sortBy,
        sortDir)));


app.MapGet("/books/{id:int}", async (int id, GetBookByIdHandler handler)
    => await handler.Handle(new GetBookByIdRequest(id)));


app.MapPut("/books/{id:int}", async (int id, UpdateBookRequest body, UpdateBookHandler handler)
    => await handler.Handle(new UpdateBookRequest(id, body.Title, body.Author, body.Year)));


app.MapDelete("/books/{id:int}", async (int id, DeleteBookHandler handler)
    => await handler.Handle(new DeleteBookRequest(id)));

app.Run();

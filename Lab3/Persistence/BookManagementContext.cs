using Microsoft.EntityFrameworkCore;
using Lab3.Features.Books;

namespace Lab3.Persistence;

public class BookManagementContext(DbContextOptions<BookManagementContext> options) : DbContext(options)
{
    public DbSet<Book> Books { get; set; }
}
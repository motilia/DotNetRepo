using Microsoft.EntityFrameworkCore;
using ProductsApi.Features.Products;

namespace ProductsApi.Persistence;

public class ApplicationContext : DbContext
{
    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
}
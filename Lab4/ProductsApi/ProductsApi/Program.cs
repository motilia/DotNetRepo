using AutoMapper;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using ProductsApi.Common.Mapping;
using ProductsApi.Common.Middleware;
using ProductsApi.Features.Products;
using ProductsApi.Features.Products.DTOs;
using ProductsApi.Persistence;
using ProductsApi.Validators;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Caching.Memory;


var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationContext>(o =>
    o.UseInMemoryDatabase("products-db"));

builder.Services.AddAutoMapper(typeof(AdvancedProductMappingProfile).Assembly);

builder.Services.AddMemoryCache();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddScoped<IValidator<CreateProductProfileRequest>, CreateProductProfileValidator>();

builder.Services.AddScoped<CreateProductHandler>();
builder.Services.AddScoped<ProductLocalizationService>();
var app = builder.Build();

app.UseMiddleware<CorrelationMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/products", async (
        CreateProductProfileRequest req,
        CreateProductHandler handler,
        CancellationToken ct) =>
    {
        var dto = await handler.Handle(req, ct);
        return Results.Created($"/products/{dto.Id}", dto);
    })
    .WithName("CreateProduct")
    .WithOpenApi();
app.MapGet("/products", async (
    [AsParameters] ProductsQuery query,
    ApplicationContext db,
    IMemoryCache cache,
    IMapper mapper,
    CancellationToken ct) =>
{
   
    string cacheKey;
    IQueryable<Product> baseQuery = db.Products.AsNoTracking();

    if (query.Category is null)
    {
        cacheKey = "products_all";
    }
    else
    {
        cacheKey = $"products_{query.Category}";
        baseQuery = baseQuery.Where(p => p.Category == query.Category.Value);
    }

    if (!cache.TryGetValue(cacheKey, out List<ProductProfileDto>? cached))
    {
        var products = await baseQuery.ToListAsync(ct);
        cached = mapper.Map<List<ProductProfileDto>>(products);

        cache.Set(cacheKey, cached, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });
    }

    return Results.Ok(cached);
})
.WithName("GetProducts")
.WithOpenApi();

app.MapGet("/product-metrics", async (ApplicationContext db, CancellationToken ct) =>
{
    var products = await db.Products
        .AsNoTracking()
        .ToListAsync(ct);

    if (products.Count == 0)
    {
        return Results.Ok(new ProductMetricsDto
        {
            TotalProducts = 0,
            TotalStock = 0,
            TotalInventoryValue = 0,
            AveragePrice = 0,
            ProductsAddedToday = 0,
            LastProductCreatedAt = null,
            Categories = new List<CategoryInventoryMetricsDto>()
        });
    }

    var totalProducts = products.Count;
    var totalStock = products.Sum(p => p.StockQuantity);
    var totalInventoryValue = products.Sum(p => p.Price * p.StockQuantity);
    var averagePrice = products.Average(p => p.Price);

    var today = DateTime.UtcNow.Date;
    var productsAddedToday = products.Count(p => p.CreatedAt.Date == today);
    var lastCreatedAt = products.Max(p => p.CreatedAt);

    var categories = products
        .GroupBy(p => p.Category)
        .Select(g => new CategoryInventoryMetricsDto
        {
            Category = g.Key,
            ProductCount = g.Count(),
            TotalStock = g.Sum(p => p.StockQuantity),
            AveragePrice = g.Average(p => p.Price)
        })
        .ToList();

    var dto = new ProductMetricsDto
    {
        TotalProducts = totalProducts,
        TotalStock = totalStock,
        TotalInventoryValue = totalInventoryValue,
        AveragePrice = averagePrice,
        ProductsAddedToday = productsAddedToday,
        LastProductCreatedAt = lastCreatedAt,
        Categories = categories
    };

    return Results.Ok(dto);
})
.WithName("GetProductMetrics")
.WithOpenApi();

app.MapGet("/products/localized", async (
        string? lang,
        ApplicationContext db,
        ProductLocalizationService localizationService,
        CancellationToken ct) =>
    {
        var products = await db.Products
            .AsNoTracking()
            .ToListAsync(ct);

        var localized = products
            .Select(p => localizationService.Localize(p, lang))
            .ToList();

        return Results.Ok(localized);
    })
    .WithName("GetLocalizedProducts")
    .WithOpenApi();

app.MapPost("/products/batch", async (
        List<CreateProductProfileRequest> requests,
        ApplicationContext db,
        IValidator<CreateProductProfileRequest> validator,
        IMapper mapper,
        IMemoryCache cache,
        ILogger<CreateProductHandler> logger,
        CancellationToken ct) =>
    {
        if (requests is null || requests.Count == 0)
        {
            return Results.BadRequest(new
            {
                Message = "Request list cannot be empty."
            });
        }

        var validEntities = new List<Product>();
        var errors = new List<object>();

        foreach (var req in requests)
        {
            var validationResult = await validator.ValidateAsync(req, ct);

            if (!validationResult.IsValid)
            {
                errors.Add(new
                {
                    req.SKU,
                    req.Name,
                    Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray()
                });
                continue;
            }

            var entity = mapper.Map<Product>(req);
            validEntities.Add(entity);
        }

        if (validEntities.Count == 0)
        {
            return Results.BadRequest(new
            {
                Message = "No valid products in batch.",
                Errors = errors
            });
        }

        
        var isInMemory = db.Database.IsInMemory(); 

        if (!isInMemory)
        {
           
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            try
            {
                await db.Products.AddRangeAsync(validEntities, ct);
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                logger.LogError(ex, "Error during batch product creation (transaction rollback)");
                throw;
            }
        }
        else
        {
            await db.Products.AddRangeAsync(validEntities, ct);
            await db.SaveChangesAsync(ct);
        }

        const string allProductsKey = "products_all";
        cache.Remove(allProductsKey);

        var affectedCategories = validEntities
            .Select(p => p.Category)
            .Distinct()
            .ToList();

        foreach (var category in affectedCategories)
        {
            var catKey = $"products_{category}";
            cache.Remove(catKey);
        }

        logger.LogInformation(
            "Batch cache invalidation performed for {AllKey} and categories {Categories}",
            allProductsKey,
            string.Join(", ", affectedCategories));

        var dtos = mapper.Map<List<ProductProfileDto>>(validEntities);

        return Results.Ok(new
        {
            CreatedCount = dtos.Count,
            FailedCount = errors.Count,
            Products = dtos,
            Failed = errors
        });
    })
    .WithName("BatchCreateProducts")
    .WithOpenApi();


app.Run();

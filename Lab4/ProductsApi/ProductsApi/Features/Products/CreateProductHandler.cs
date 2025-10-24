using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using ProductsApi.Common.Logging;
using ProductsApi.Features.Products.DTOs;
using ProductsApi.Persistence;
namespace ProductsApi.Features.Products;
public class CreateProductHandler
{
    private readonly ApplicationContext _db;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateProductHandler> _logger;
    private readonly IMemoryCache _cache;

    public CreateProductHandler(ApplicationContext db, IMapper mapper, ILogger<CreateProductHandler> logger, IMemoryCache cache)
    { _db = db; _mapper = mapper; _logger = logger; _cache = cache; }

    public async Task<ProductProfileDto> Handle(CreateProductProfileRequest req, CancellationToken ct = default)
    {
        var opId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogInformation(new EventId(2001, "ProductCreationStarted"),
            "Product creation started: Name='{Name}', Brand='{Brand}', SKU={SKU}, Category={Category}, OpId={OpId}",
            req.Name, req.Brand, req.SKU, req.Category, opId);

       
        _logger.LogInformation(new EventId(2007, "SKUValidationPerformed"), "Validating SKU uniqueness: {SKU}", req.SKU);
        if (await _db.Products.AnyAsync(p => p.SKU == req.SKU, ct))
        {
            _logger.LogWarning(new EventId(2002, "ProductValidationFailed"), "SKU already exists: {SKU}", req.SKU);
            throw new InvalidOperationException("SKU already exists.");
        }

        var entity = _mapper.Map<Product>(req);

        _logger.LogInformation(new EventId(2004, "DatabaseOperationStarted"), "Saving product… OpId={OpId}", opId);
        _db.Products.Add(entity);
        await _db.SaveChangesAsync(ct);

        _cache.Remove("all_products");
        _logger.LogInformation(new EventId(2006, "CacheOperationPerformed"), "Cache invalidated for key 'all_products'.");

        var dto = _mapper.Map<ProductProfileDto>(entity);
        _logger.LogInformation(new EventId(2003, "ProductCreationCompleted"), "Product created: {Id}", entity.Id);
        return dto;
    }
}

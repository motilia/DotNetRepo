using System.Diagnostics;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ProductsApi.Common.Logging;
using ProductsApi.Features.Products.DTOs;
using ProductsApi.Persistence;

namespace ProductsApi.Features.Products;

public class CreateProductHandler
{
    private readonly IMapper _mapper;
    private readonly ApplicationContext _db;
    private readonly IMemoryCache _cache;
    private readonly IValidator<CreateProductProfileRequest> _validator;
    private readonly ILogger<CreateProductHandler> _logger;

    private const string CacheKeyAllProducts = "all_products";

    public CreateProductHandler(
        IMapper mapper,
        ApplicationContext db,
        IMemoryCache cache,
        IValidator<CreateProductProfileRequest> validator,
        ILogger<CreateProductHandler> logger)
    {
        _mapper = mapper;
        _db = db;
        _cache = cache;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ProductProfileDto> Handle(CreateProductProfileRequest request, CancellationToken ct = default)
    {
        var operationId = Guid.NewGuid().ToString("N")[..8];
        var totalSw = Stopwatch.StartNew();

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["OperationId"] = operationId,
            ["SKU"] = request.SKU,
            ["Category"] = request.Category.ToString()
        });

        _logger.LogInformation(
            new EventId(ProductLogEvents.ProductCreationStarted, nameof(ProductLogEvents.ProductCreationStarted)),
            "Product creation started for {Name} / {Brand} (SKU={SKU}, Category={Category})",
            request.Name, request.Brand, request.SKU, request.Category);

        var metrics = new ProductCreationMetrics(
            OperationId: operationId,
            ProductName: request.Name,
            SKU: request.SKU,
            Category: request.Category,
            ValidationDuration: TimeSpan.Zero,
            DatabaseSaveDuration: TimeSpan.Zero,
            TotalDuration: TimeSpan.Zero,
            Success: false,
            ErrorReason: null
        );

        try
        {
            
            var validationSw = Stopwatch.StartNew();

            _logger.LogInformation(
                new EventId(ProductLogEvents.SKUValidationPerformed, nameof(ProductLogEvents.SKUValidationPerformed)),
                "SKU validation started for {SKU}", request.SKU);

            var validationResult = await _validator.ValidateAsync(request, ct);

            _logger.LogInformation(
                new EventId(ProductLogEvents.StockValidationPerformed, nameof(ProductLogEvents.StockValidationPerformed)),
                "Stock validation performed for {SKU} with quantity {Stock}",
                request.SKU, request.StockQuantity);

            validationSw.Stop();
            metrics = metrics with { ValidationDuration = validationSw.Elapsed };

            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));

                metrics = metrics with
                {
                    Success = false,
                    ErrorReason = errors
                };

                _logger.LogWarning(
                    new EventId(ProductLogEvents.ProductValidationFailed, nameof(ProductLogEvents.ProductValidationFailed)),
                    "Validation failed for SKU {SKU}: {Errors}",
                    request.SKU, errors);

                _logger.LogProductCreationMetrics(metrics);

                throw new ValidationException(validationResult.Errors);
            }

          
            var dbSw = Stopwatch.StartNew();

            _logger.LogInformation(
                new EventId(ProductLogEvents.DatabaseOperationStarted, nameof(ProductLogEvents.DatabaseOperationStarted)),
                "Database operation started for SKU {SKU}", request.SKU);

            var entity = _mapper.Map<Product>(request);

            await _db.Products.AddAsync(entity, ct);
            await _db.SaveChangesAsync(ct);

            dbSw.Stop();
            metrics = metrics with { DatabaseSaveDuration = dbSw.Elapsed };

            _logger.LogInformation(
                new EventId(ProductLogEvents.DatabaseOperationCompleted, nameof(ProductLogEvents.DatabaseOperationCompleted)),
                "Database operation completed for ProductId {ProductId}, SKU {SKU}",
                entity.Id, entity.SKU);

            _cache.Remove(CacheKeyAllProducts);

            _logger.LogInformation(
                new EventId(ProductLogEvents.CacheOperationPerformed, nameof(ProductLogEvents.CacheOperationPerformed)),
                "Cache operation performed for key {CacheKey}", CacheKeyAllProducts);

            var dto = _mapper.Map<ProductProfileDto>(entity);

            totalSw.Stop();
            metrics = metrics with
            {
                TotalDuration = totalSw.Elapsed,
                Success = true
            };

            _logger.LogProductCreationMetrics(metrics);

            _logger.LogInformation(
                new EventId(ProductLogEvents.ProductCreationCompleted, nameof(ProductLogEvents.ProductCreationCompleted)),
                "Product creation completed for SKU {SKU} in {TotalMs} ms",
                request.SKU, metrics.TotalDuration.TotalMilliseconds);

            return dto;
        }
        catch (Exception ex)
        {
            totalSw.Stop();
            metrics = metrics with
            {
                TotalDuration = totalSw.Elapsed,
                Success = false,
                ErrorReason = ex.Message
            };

            _logger.LogError(ex,
                "Error during product creation for SKU {SKU}",
                request.SKU);

            _logger.LogProductCreationMetrics(metrics);

            throw;
        }
    }
}

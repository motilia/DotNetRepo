using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using ProductsApi.Common.Logging;
using ProductsApi.Common.Mapping;
using ProductsApi.Features.Products;
using ProductsApi.Features.Products.DTOs;
using ProductsApi.Persistence;
using ProductsApi.Validators;
using Xunit;

namespace ProductsApi.Tests;

public class CreateProductHandlerIntegrationTests : IDisposable
{
    private readonly ApplicationContext _db;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly IValidator<CreateProductProfileRequest> _validator;
    private readonly Mock<ILogger<CreateProductHandler>> _handlerLoggerMock;
    private readonly CreateProductHandler _handler;

    public CreateProductHandlerIntegrationTests()
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(databaseName: $"products-db-tests-{Guid.NewGuid()}")
            .Options;

        _db = new ApplicationContext(dbOptions);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(typeof(AdvancedProductMappingProfile).Assembly);
        });

        _mapper = mapperConfig.CreateMapper();

        _cache = new MemoryCache(new MemoryCacheOptions());

        var validatorLogger = new LoggerFactory().CreateLogger<CreateProductProfileValidator>();
        _validator = new CreateProductProfileValidator(_db, validatorLogger);

        _handlerLoggerMock = new Mock<ILogger<CreateProductHandler>>();

        _handler = new CreateProductHandler(
            _mapper,
            _db,
            _cache,
            _validator,
            _handlerLoggerMock.Object);
    }


    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
        _cache.Dispose();
    }



    [Fact]
    public async Task Handle_ValidElectronicsProductRequest_CreatesProductWithCorrectMappings()
    {
       
        var request = new CreateProductProfileRequest
        {
            Name = "Smart Gaming Laptop",          
            Brand = "Mega Tech",                   
            SKU = "ELC-TEST-001",
            Category = ProductCategory.Electronics,
            Price = 1999.99m,                      
            ReleaseDate = DateTime.UtcNow.AddYears(-2), 
            ImageUrl = "https://example.com/laptop.png",
            StockQuantity = 8
        };


        var dto = await _handler.Handle(request, CancellationToken.None);

        dto.Should().NotBeNull();
        dto.Name.Should().Be("Smart Gaming Laptop");
        dto.Brand.Should().Be("Mega Tech");
        dto.SKU.Should().Be("ELC-TEST-001");


        dto.CategoryDisplayName.Should().Be("Electronics & Technology");

        dto.BrandInitials.Should().Be("MT");

        dto.ProductAge.Should().NotBeNullOrWhiteSpace();

        dto.FormattedPrice.Should().NotBeNullOrWhiteSpace();

        dto.AvailabilityStatus.Should().NotBeNullOrWhiteSpace();

        _handlerLoggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.Is<EventId>(e => e.Id == ProductLogEvents.ProductCreationStarted),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Product creation started")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }



    [Fact]
    public async Task Handle_DuplicateSKU_ThrowsValidationExceptionWithLogging()
    {
        
        var existingProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Existing Phone",
            Brand = "PhoneCo",
            SKU = "DUP-SKU-001",
            Category = ProductCategory.Electronics,
            Price = 799.99m,
            ReleaseDate = DateTime.UtcNow.AddYears(-1),
            ImageUrl = "https://example.com/phone.png",
            StockQuantity = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _db.Products.AddAsync(existingProduct);
        await _db.SaveChangesAsync();

        var request = new CreateProductProfileRequest
        {
            Name = "New Phone Model",
            Brand = "PhoneCo",
            SKU = "DUP-SKU-001", 
            Category = ProductCategory.Electronics,
            Price = 899.99m,
            ReleaseDate = DateTime.UtcNow.AddMonths(-3),
            ImageUrl = "https://example.com/new-phone.png",
            StockQuantity = 4
        };

     
        var act = async () => await _handler.Handle(request, CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ValidationException>(act);

        ex.Message.Should().Contain("already exists");

        _handlerLoggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.Is<EventId>(e => e.Id == ProductLogEvents.ProductValidationFailed),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validation failed")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }



    [Fact]
    public async Task Handle_HomeProductRequest_AppliesDiscountAndConditionalMapping()
    {
        
        var request = new CreateProductProfileRequest
        {
            Name = "Aromatherapy Diffuser",
            Brand = "Home Bliss",
            SKU = "HOME-DIFF-001",
            Category = ProductCategory.Home,
            Price = 200m, 
            ReleaseDate = new DateTime(2024, 1, 1),
            ImageUrl = "https://example.com/diffuser.png",
            StockQuantity = 6
        };

       
        var dto = await _handler.Handle(request, CancellationToken.None);

        
        dto.Should().NotBeNull();

        dto.CategoryDisplayName.Should().Be("Home & Garden");

        dto.Price.Should().Be(180m);

        dto.ImageUrl.Should().BeNull();
    }
}

using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductsApi.Features.Products;
using ProductsApi.Features.Products.DTOs;
using ProductsApi.Persistence;

namespace ProductsApi.Validators;

public class CreateProductProfileValidator : AbstractValidator<CreateProductProfileRequest>
{
    private readonly ApplicationContext _db;
    private readonly ILogger<CreateProductProfileValidator> _logger;

    public CreateProductProfileValidator(ApplicationContext db, ILogger<CreateProductProfileValidator> logger)
    {
        _db = db;
        _logger = logger;
        
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .Length(1, 200)
            .Must(BeValidName).WithMessage("Product name contains inappropriate content.")
            .MustAsync(BeUniqueName).WithMessage("A product with the same Name and Brand already exists.");
        
        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("Brand is required.")
            .Length(2, 100)
            .Must(BeValidBrandName).WithMessage("Brand contains invalid characters.");
        
        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("SKU is required.")
            .Must(BeValidSKU).WithMessage("SKU must be 5-20 chars, alphanumeric or hyphen.")
            .MustAsync(BeUniqueSKU).WithMessage("SKU already exists.");

  
        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Invalid product category.");

     
        RuleFor(x => x.Price)
            .GreaterThan(0m).WithMessage("Price must be > 0.")
            .LessThan(10_000m).WithMessage("Price must be < 10,000.");

      
        RuleFor(x => x.ReleaseDate)
            .Must(d => d >= new DateTime(1900,1,1)).WithMessage("Release date cannot be before 1900.")
            .Must(d => d <= DateTime.UtcNow).WithMessage("Release date cannot be in the future.");

 
        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock cannot be negative.")
            .LessThanOrEqualTo(100_000).WithMessage("Stock too large.");
        
        When(x => !string.IsNullOrWhiteSpace(x.ImageUrl), () =>
        {
            RuleFor(x => x.ImageUrl!)
                .Must(BeValidImageUrl).WithMessage("ImageUrl must be HTTP/HTTPS and end with .jpg/.jpeg/.png/.gif/.webp");
        });

        When(x => x.Category == ProductCategory.Electronics, () =>
        {
            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(50m).WithMessage("Electronics must be at least $50.");

            RuleFor(x => x.Name)
                .Must(ContainTechnologyKeywords).WithMessage("Electronics name must contain a technology keyword.");

            RuleFor(x => x.ReleaseDate)
                .Must(d => d >= DateTime.UtcNow.AddYears(-5)).WithMessage("Electronics must be released within last 5 years.");
        });

        When(x => x.Category == ProductCategory.Home, () =>
        {
            RuleFor(x => x.Price)
                .LessThanOrEqualTo(200m).WithMessage("Home products must be at most $200.");

            RuleFor(x => x.Name)
                .Must(BeAppropriateForHome).WithMessage("Home product name is not appropriate.");
        });

        When(x => x.Category == ProductCategory.Clothing, () =>
        {
            RuleFor(x => x.Brand)
                .MinimumLength(3).WithMessage("Clothing brand must be at least 3 characters.");
        });

       
        When(x => x.Price > 100m, () =>
        {
            RuleFor(x => x.StockQuantity)
                .LessThanOrEqualTo(20).WithMessage("For price > $100, stock must be ≤ 20.");
        });

        
        RuleFor(x => x)
            .MustAsync(PassBusinessRules).WithMessage("Business rules failed.");
    }

 
    private static readonly HashSet<string> InappropriateWords =
        new(StringComparer.OrdinalIgnoreCase) { "badword", "adult", "nsfw", "weapon", "explosive" };

    private static readonly HashSet<string> HomeRestrictedWords =
        new(StringComparer.OrdinalIgnoreCase) { "weapon", "hazardous", "toxic" };

    private static readonly string[] TechKeywords =
        { "smart", "wifi", "bluetooth", "oled", "4k", "8k", "ssd", "laptop", "phone", "camera", "usb", "type-c", "gaming" };

    private bool BeValidName(string name)
        => InappropriateWords.All(w => !name.Contains(w, StringComparison.OrdinalIgnoreCase));

    private async Task<bool> BeUniqueName(CreateProductProfileRequest req, string name, CancellationToken ct)
    {
        var exists = await _db.Products
            .AnyAsync(p => p.Name == req.Name && p.Brand == req.Brand, ct);
        if (exists)
            _logger.LogInformation("Name+Brand duplicate check failed for {Name}/{Brand}", req.Name, req.Brand);
        return !exists;
    }

    private bool BeValidBrandName(string brand)
        => Regex.IsMatch(brand, @"^[\p{L}\p{M}0-9][\p{L}\p{M}0-9 .'\-]*$"); 

    private bool BeValidSKU(string sku)
        => Regex.IsMatch(sku.Trim(), @"^[A-Za-z0-9-]{5,20}$");

    private async Task<bool> BeUniqueSKU(string sku, CancellationToken ct)
    {
        var exists = await _db.Products.AnyAsync(p => p.SKU == sku, ct);
        if (exists)
            _logger.LogInformation("SKU duplicate check failed for {SKU}", sku);
        return !exists;
    }

    private bool BeValidImageUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme is not ("http" or "https")) return false;
        return uri.AbsolutePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            || uri.AbsolutePath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
            || uri.AbsolutePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || uri.AbsolutePath.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
            || uri.AbsolutePath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);
    }

    private bool ContainTechnologyKeywords(string name)
        => TechKeywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase));

    private bool BeAppropriateForHome(string name)
        => HomeRestrictedWords.All(w => !name.Contains(w, StringComparison.OrdinalIgnoreCase));

  
    private async Task<bool> PassBusinessRules(CreateProductProfileRequest req, CancellationToken ct)
    {
        
        var todayUtc = DateTime.UtcNow.Date;
        var addedToday = await _db.Products.CountAsync(p => p.CreatedAt.Date == todayUtc, ct);
        if (addedToday >= 500)
        {
            _logger.LogWarning("Daily product limit reached: {Count}", addedToday);
            return false;
        }
        
        if (req.Category == ProductCategory.Electronics && req.Price < 50m)
            return false;

        
        if (req.Category == ProductCategory.Home && !BeAppropriateForHome(req.Name))
            return false;

        
        if (req.Price > 500m && req.StockQuantity > 10)
            return false;
        
        return true;
    }
}

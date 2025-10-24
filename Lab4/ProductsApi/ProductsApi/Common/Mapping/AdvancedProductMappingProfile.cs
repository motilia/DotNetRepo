using AutoMapper;
using ProductsApi.Features.Products;
using ProductsApi.Features.Products.DTOs;

namespace ProductsApi.Common.Mapping;

public class AdvancedProductMappingProfile : Profile
{
    public AdvancedProductMappingProfile()
    {
       
        CreateMap<CreateProductProfileRequest, Product>()
            .ForMember(d => d.Id,         m => m.MapFrom(_ => Guid.NewGuid()))
            .ForMember(d => d.CreatedAt,  m => m.MapFrom(_ => DateTime.UtcNow))
            .ForMember(d => d.IsAvailable,m => m.MapFrom(s => s.StockQuantity > 0))
            .ForMember(d => d.UpdatedAt,  m => m.Ignore())
          
            .ForMember(d => d.Price,      m => m.MapFrom(s =>
                s.Category == ProductCategory.Home ? Math.Round(s.Price * 0.9m, 2) : s.Price))
           
            .ForMember(d => d.ImageUrl,   m => m.MapFrom(s =>
                s.Category == ProductCategory.Home ? null : s.ImageUrl));

       
        CreateMap<Product, ProductProfileDto>()
            .ForMember(d => d.CategoryDisplayName, m => m.MapFrom<CategoryDisplayResolver>())
            .ForMember(d => d.FormattedPrice,      m => m.MapFrom<PriceFormatterResolver>())
            .ForMember(d => d.ProductAge,          m => m.MapFrom<ProductAgeResolver>())
            .ForMember(d => d.BrandInitials,       m => m.MapFrom<BrandInitialsResolver>())
            .ForMember(d => d.AvailabilityStatus,  m => m.MapFrom<AvailabilityStatusResolver>());
    }
}

public class CategoryDisplayResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product s, ProductProfileDto d, string _, ResolutionContext __) =>
        s.Category switch
        {
            ProductCategory.Electronics => "Electronics & Technology",
            ProductCategory.Clothing    => "Clothing & Fashion",
            ProductCategory.Books       => "Books & Media",
            ProductCategory.Home        => "Home & Garden",
            _ => "Uncategorized"
        };
}

public class PriceFormatterResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product s, ProductProfileDto d, string _, ResolutionContext __) =>
        s.Price.ToString("C2");
}

public class ProductAgeResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product s, ProductProfileDto d, string _, ResolutionContext __)
    {
        var days = (DateTime.UtcNow.Date - s.ReleaseDate.Date).TotalDays;
        if (days < 30)  return "New Release";
        if (days < 365) return $"{(int)(days/30)} months old";
        if (days < 1825) return $"{(int)(days/365)} years old";
        return "Classic";
    }
}

public class BrandInitialsResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product s, ProductProfileDto d, string _, ResolutionContext __)
    {
        if (string.IsNullOrWhiteSpace(s.Brand)) return "?";
        var parts = s.Brand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return char.ToUpperInvariant(parts[0][0]).ToString();
        return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}";
    }
}

public class AvailabilityStatusResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product s, ProductProfileDto d, string _, ResolutionContext __)
    {
        if (!s.IsAvailable) return "Out of Stock";
        if (s.StockQuantity == 0) return "Unavailable";
        if (s.StockQuantity == 1) return "Last Item";
        if (s.StockQuantity <= 5) return "Limited Stock";
        return "In Stock";
    }
}

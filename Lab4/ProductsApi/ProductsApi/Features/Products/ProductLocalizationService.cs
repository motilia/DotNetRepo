using System.Globalization;
using System.Resources;
using ProductsApi.Features.Products.DTOs;
using ProductsApi.Persistence;

namespace ProductsApi.Features.Products;

public class ProductLocalizationService
{
    private readonly ApplicationContext _db;
    private readonly ResourceManager _resourceManager;

    public ProductLocalizationService(ApplicationContext db)
    {
        _db = db;

        // baza trebuie să fie: <DefaultNamespace>.Resources.ProductResources
        _resourceManager = new ResourceManager(
            "ProductsApi.Resources.ProductResources",
            typeof(ProductLocalizationService).Assembly);
    }

    public LocalizedProductProfileDto Localize(Product product, string? language)
    {
        var culture = !string.IsNullOrWhiteSpace(language)
            ? new CultureInfo(language)
            : CultureInfo.CurrentUICulture;

        var nameKey = $"Product_{product.SKU}_Name";
        var descKey = $"Product_{product.SKU}_Desc";

        var localizedName =
            _resourceManager.GetString(nameKey, culture) ?? product.Name;

        var localizedDescription =
            _resourceManager.GetString(descKey, culture) ?? "No description available in this language.";

        var categoryDisplayName = product.Category switch
        {
            ProductCategory.Electronics => "Electronics & Technology",
            ProductCategory.Home        => "Home & Garden",
            ProductCategory.Clothing    => "Clothing & Fashion",
            ProductCategory.Books       => "Books & Media",
            _                           => product.Category.ToString()
        };

        return new LocalizedProductProfileDto
        {
            Id = product.Id,
            SKU = product.SKU,
            Brand = product.Brand,
            CategoryDisplayName = categoryDisplayName,
            Language = culture.Name,
            Name = localizedName,
            Description = localizedDescription
        };
    }
}

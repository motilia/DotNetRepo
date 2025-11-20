using ProductsApi.Features.Products;

namespace ProductsApi.Features.Products.DTOs;

public class CategoryInventoryMetricsDto
{
    public ProductCategory Category { get; set; }
    public int ProductCount { get; set; }
    public int TotalStock { get; set; }
    public decimal AveragePrice { get; set; }
}

public class ProductMetricsDto
{
    public int TotalProducts { get; set; }
    public int TotalStock { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public decimal AveragePrice { get; set; }

    public int ProductsAddedToday { get; set; }
    public DateTime? LastProductCreatedAt { get; set; }

    public List<CategoryInventoryMetricsDto> Categories { get; set; } = new();
}

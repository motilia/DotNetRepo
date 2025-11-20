namespace ProductsApi.Features.Products.DTOs;

public class LocalizedProductProfileDto
{
    public Guid Id { get; set; }
    public string SKU { get; set; } = default!;
    public string Brand { get; set; } = default!;
    public string CategoryDisplayName { get; set; } = default!;

    public string Language { get; set; } = default!;
    public string Name { get; set; } = default!;              
    public string Description { get; set; } = default!;       
}

using System.ComponentModel.DataAnnotations;
using ProductsApi.Features.Products;

namespace ProductsApi.Validators.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ProductCategoryAttribute : ValidationAttribute
{
    private readonly ProductCategory[] _allowed;
    public ProductCategoryAttribute(params ProductCategory[] allowed) => _allowed = allowed;

    public override bool IsValid(object? value)
    {
        if (value is null) return false;
        if (value is ProductCategory c) return _allowed.Contains(c);
        return false;
    }

    public override string FormatErrorMessage(string name)
        => $"{name} must be one of: {string.Join(", ", _allowed)}";
}
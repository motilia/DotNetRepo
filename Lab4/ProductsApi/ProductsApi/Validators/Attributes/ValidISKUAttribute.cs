using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ProductsApi.Validators.Attributes;

public class ValidSKUAttribute : ValidationAttribute, IClientModelValidator
{
    public override bool IsValid(object? value)
    {
        if (value is null) return true;
        var sku = value.ToString()!.Trim();
        return Regex.IsMatch(sku, @"^[A-Za-z0-9-]{5,20}$");
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        Merge(context.Attributes, "data-val", "true");
        Merge(context.Attributes, "data-val-validsku", ErrorMessage ?? "Invalid SKU format.");
    }

    private static void Merge(IDictionary<string, string> attrs, string key, string value)
    {
        if (!attrs.ContainsKey(key)) attrs.Add(key, value);
    }
}
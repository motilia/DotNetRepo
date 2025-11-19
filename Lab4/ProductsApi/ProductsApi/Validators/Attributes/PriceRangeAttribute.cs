using System.ComponentModel.DataAnnotations;

namespace ProductsApi.Validators.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class PriceRangeAttribute : ValidationAttribute
{
    private readonly decimal _min;
    private readonly decimal _max;

    public PriceRangeAttribute(double min, double max)
    {
        _min = (decimal)min;
        _max = (decimal)max;
    }

    public override bool IsValid(object? value)
    {
        if (value is null) return true;
        if (value is decimal d) return d >= _min && d <= _max;
        return false;
    }

    public override string FormatErrorMessage(string name)
        => $"{name} must be between {_min:C2} and {_max:C2}.";
}
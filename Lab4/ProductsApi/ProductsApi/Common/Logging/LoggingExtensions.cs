using Microsoft.Extensions.Logging;

namespace ProductsApi.Common.Logging;

public static class LoggingExtensions
{
    public static void LogProductCreationMetrics(
        this ILogger logger,
        ProductCreationMetrics metrics)
    {
        logger.LogInformation(
            new EventId(ProductLogEvents.ProductCreationCompleted, nameof(ProductLogEvents.ProductCreationCompleted)),
            "Product metrics: OperationId={OperationId}, Name={Name}, SKU={SKU}, Category={Category}, " +
            "ValidationMs={ValidationMs}, DbMs={DbMs}, TotalMs={TotalMs}, Success={Success}, Error={Error}",
            metrics.OperationId,
            metrics.ProductName,
            metrics.SKU,
            metrics.Category.ToString(),
            metrics.ValidationDuration.TotalMilliseconds,
            metrics.DatabaseSaveDuration.TotalMilliseconds,
            metrics.TotalDuration.TotalMilliseconds,
            metrics.Success,
            metrics.ErrorReason ?? string.Empty
        );
    }
}

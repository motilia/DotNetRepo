using System.Net;
using System.Text.Json;
using FluentValidation;
using Lab3.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Lab3.Common.Middleware;

public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IHostEnvironment env)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException nf)
        {
            await WriteProblem(context, StatusCodes.Status404NotFound, nf.Message);
        }
        catch (BadRequestDomainException br)
        {
            await WriteProblem(context, StatusCodes.Status400BadRequest, br.Message);
        }
        catch (ValidationException ve)
        {
            var pd = new ValidationProblemDetails(
                ve.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed"
            };
            await WriteProblem(context, pd);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            var message = env.IsDevelopment() ? ex.ToString() : "Unexpected error";
            await WriteProblem(context, StatusCodes.Status500InternalServerError, message);
        }
    }

    private static async Task WriteProblem(HttpContext ctx, int status, string title)
        => await WriteProblem(ctx, new ProblemDetails { Status = status, Title = title });

    private static async Task WriteProblem(HttpContext ctx, ProblemDetails problem)
    {
        ctx.Response.ContentType = "application/problem+json";
        ctx.Response.StatusCode = problem.Status ?? (int)HttpStatusCode.InternalServerError;
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
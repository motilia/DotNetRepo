using AutoMapper;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using ProductsApi.Common.Mapping;
using ProductsApi.Common.Middleware;
using ProductsApi.Features.Products;
using ProductsApi.Features.Products.DTOs;
using ProductsApi.Persistence;
using ProductsApi.Validators;
using Microsoft.Extensions.Logging.Console;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationContext>(o =>
    o.UseInMemoryDatabase("products-db"));

builder.Services.AddAutoMapper(typeof(AdvancedProductMappingProfile).Assembly);

builder.Services.AddMemoryCache();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddScoped<IValidator<CreateProductProfileRequest>, CreateProductProfileValidator>();

builder.Services.AddScoped<CreateProductHandler>();

var app = builder.Build();

app.UseMiddleware<CorrelationMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/products", async (
        CreateProductProfileRequest req,
        CreateProductHandler handler,
        CancellationToken ct) =>
    {
        var dto = await handler.Handle(req, ct);
        return Results.Created($"/products/{dto.Id}", dto);
    })
    .WithName("CreateProduct")
    .WithOpenApi();

app.Run();

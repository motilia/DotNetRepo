using AutoMapper;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using ProductsApi.Common.Mapping;
using ProductsApi.Features.Products;
using ProductsApi.Features.Products.DTOs;
using ProductsApi.Persistence;
using ProductsApi.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddDbContext<ApplicationContext>(o => o.UseInMemoryDatabase("products-db"));


builder.Services.AddAutoMapper(typeof(AdvancedProductMappingProfile).Assembly);


builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddScoped<IValidator<CreateProductProfileRequest>, CreateProductProfileValidator>();


builder.Services.AddScoped<CreateProductHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.MapPost("/products", async (
        CreateProductProfileRequest req,
        IValidator<CreateProductProfileRequest> validator,
        CreateProductHandler handler,
        CancellationToken ct) =>
    {
        var validationResult = await validator.ValidateAsync(req, ct);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var dto = await handler.Handle(req, ct);
        return Results.Created($"/products/{dto.Id}", dto);
    })
    .WithName("CreateProduct")
    .WithOpenApi();
app.MapGet("/products", async (ApplicationContext db, CancellationToken ct) =>
    await db.Products.ToListAsync(ct)
).WithName("GetProducts").WithOpenApi();

app.Run();
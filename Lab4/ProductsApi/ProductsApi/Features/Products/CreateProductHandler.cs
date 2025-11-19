using AutoMapper;
using ProductsApi.Features.Products.DTOs;
using ProductsApi.Persistence;

namespace ProductsApi.Features.Products;

public class CreateProductHandler
{
    private readonly IMapper _mapper;
    private readonly ApplicationContext _db;

    public CreateProductHandler(IMapper mapper, ApplicationContext db)
    {
        _mapper = mapper;
        _db = db;
    }

    public async Task<ProductProfileDto> Handle(CreateProductProfileRequest request, CancellationToken ct = default)
    {
        
        var entity = _mapper.Map<Product>(request);

        await _db.Products.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);


        var dto = _mapper.Map<ProductProfileDto>(entity);
        return dto;
    }
}
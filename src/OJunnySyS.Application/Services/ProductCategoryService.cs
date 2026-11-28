using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Catalog;

namespace OJunnySyS.Application.Services;

public sealed class ProductCategoryService(IProductCategoryRepository repository)
{
    public async Task<IReadOnlyList<ProductCategoryDto>> GetAsync(bool includeInactive = false, CancellationToken ct = default) =>
        (await repository.GetProductCategoriesAsync(includeInactive, ct)).Select(Map).ToArray();

    public async Task<ProductCategoryDto> CreateAsync(CreateProductCategoryRequest request, CancellationToken ct = default) =>
        Map(await repository.CreateProductCategoryAsync(request.Name, request.ParentCategoryId, ct));

    private static ProductCategoryDto Map(ProductCategory category) =>
        new(category.Id, category.Name, category.ParentCategoryId, category.ParentCategory?.Name,
            category.DisplayPath, category.IsActive, category.Revision);
}

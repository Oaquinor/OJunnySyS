using OJunnySyS.Domain.Catalog;

namespace OJunnySyS.Application.Abstractions;

public interface IProductCategoryRepository
{
    Task<IReadOnlyList<ProductCategory>> GetProductCategoriesAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<ProductCategory> CreateProductCategoryAsync(string name, Guid? parentCategoryId = null, CancellationToken cancellationToken = default);
}

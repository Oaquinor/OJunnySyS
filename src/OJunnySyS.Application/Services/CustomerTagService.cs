using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Common;

namespace OJunnySyS.Application.Services;

public sealed class CustomerTagService(ICustomerTagRepository repository)
{
    public Task<IReadOnlyList<CustomerTagDto>> GetTagsAsync(CancellationToken ct = default) => repository.GetTagsAsync(ct);
    public Task<IReadOnlyList<CustomerTagAssignmentDto>> GetCustomerTagsAsync(Guid customerId, CancellationToken ct = default) => repository.GetCustomerTagsAsync(customerId, ct);
    public Task<CustomerTagAssignmentDto> AddToCustomerAsync(Guid customerId, AddCustomerTagRequest request, Guid actorId, CancellationToken ct = default)
    {
        if (customerId == Guid.Empty) throw new DomainException("Selecciona un cliente válido.");
        if (actorId == Guid.Empty) throw new DomainException("No se pudo identificar a la persona que asigna la etiqueta.");
        if (request is null) throw new DomainException("La etiqueta es obligatoria.");
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 40)
            throw new DomainException("La etiqueta debe tener entre 1 y 40 caracteres.");
        if (!Enum.IsDefined(request.Color)) throw new DomainException("El color de la etiqueta no es válido.");
        return repository.AddToCustomerAsync(customerId, request, actorId, ct);
    }
    public Task RemoveFromCustomerAsync(Guid customerId, Guid tagId, CancellationToken ct = default) => repository.RemoveFromCustomerAsync(customerId, tagId, ct);
}

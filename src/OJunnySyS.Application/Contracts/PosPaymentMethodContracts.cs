using OJunnySyS.Domain.Sales;

namespace OJunnySyS.Application.Contracts;

public sealed record TenantPaymentMethodDto(Guid Id, string Code, string Name, PaymentMethodType Type,
    string? Instructions, string? AccountLabel, string? AccountReference, string? QrPayload, bool IsActive, long Revision);
public sealed record CreatePaymentMethodRequest(string Name, PaymentMethodType Type, string? Instructions = null,
    string? AccountLabel = null, string? AccountReference = null, string? QrPayload = null);
public sealed record UpdatePaymentMethodRequest(string Name, string? Instructions, string? AccountLabel,
    string? AccountReference, string? QrPayload, long ExpectedRevision);
public sealed record SetPaymentMethodActiveRequest(bool IsActive, long ExpectedRevision);

public static class PosPaymentMethodMappings
{
    public static TenantPaymentMethodDto ToDto(this TenantPaymentMethod method) => new(method.Id, method.Code, method.Name,
        method.Type, method.Instructions, method.AccountLabel, method.AccountReference, method.QrPayload, method.IsActive, method.Revision);
}

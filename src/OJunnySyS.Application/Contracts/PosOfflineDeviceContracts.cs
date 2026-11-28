using OJunnySyS.Domain.Pos;
using OJunnySyS.Domain.Sales;

namespace OJunnySyS.Application.Contracts;

public sealed record RegisterPosOfflineDeviceRequest(Guid DeviceId, string Label, string PublicKey);
public sealed record RevokePosOfflineDeviceRequest(long ExpectedRevision);
public sealed record PosOfflineDeviceDto(Guid DeviceId, string Label, string Fingerprint, bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset? LastUsedAtUtc, long Revision);
public sealed record OfflineCheckoutRequest(CheckoutRequest Checkout, Guid DeviceId, string Payload, string Signature);

/// <summary>Canonical byte-for-byte data signed by the enrolled device key before an offline draft is submitted.</summary>
public static class PosOfflineCheckoutPayload
{
    public static string Create(Guid tenantId, Guid userId, Guid deviceId, CheckoutRequest request)
    {
        static string Id(Guid? value) => value?.ToString("N") ?? string.Empty;
        static string Field(string? value) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value ?? string.Empty));
        static string Amount(decimal value) => value.ToString("G29", System.Globalization.CultureInfo.InvariantCulture);
        var values = new List<string>
        {
            "ojunnysys-pos-offline-v1", tenantId.ToString("N"), userId.ToString("N"), deviceId.ToString("N"),
            request.IdempotencyKey.ToString("N"), OJunnySyS.Domain.Pos.PosRegister.NormalizeCode(request.RegisterCode),
            Id(request.CustomerId), request.PaymentMethod.ToString(), Id(request.SellerUserId), request.DueDate?.ToString("yyyy-MM-dd") ?? string.Empty,
            Id(request.SalesQuoteId), request.SalesQuoteRevision?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            Id(request.ParkedSaleId)
        };
        values.AddRange(request.Lines.OrderBy(x => x.ProductId).ThenBy(x => x.Quantity)
            .SelectMany(x => new[] { x.ProductId.ToString("N"), x.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture), Amount(x.DiscountPercent) }));
        values.Add("payments");
        values.AddRange((request.Payments ?? []).OrderBy(x => x.PaymentMethodId).ThenBy(x => x.Amount)
            .SelectMany(x => new[] { x.PaymentMethodId.ToString("N"), Amount(x.Amount), Field(x.Reference) }));
        return string.Join('.', values.Select(Field));
    }
}

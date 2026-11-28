using System.Net;
using System.Net.Http.Json;
using OJunnySyS.Application.Contracts;

namespace OJunnySyS.PosClient.Services;

public sealed class PosApiClient(HttpClient http)
{
    public Task<IReadOnlyList<PosRegisterDto>> GetRegistersAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PosRegisterDto>>("api/v1/pos/client/terminals", ct);

    public Task<IReadOnlyList<TenantPaymentMethodDto>> GetPaymentOptionsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<TenantPaymentMethodDto>>("api/v1/pos/client/payment-options", ct);

    public Task<IReadOnlyList<TenantMemberDto>> GetSellersAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<TenantMemberDto>>("api/v1/pos/client/sellers", ct);

    public Task<TenantBusinessSettingsDto> GetBusinessSettingsAsync(CancellationToken ct = default) =>
        GetAsync<TenantBusinessSettingsDto>("api/v1/pos/client/business-settings", ct);

    public Task<IReadOnlyList<ProductDto>> SearchProductsAsync(Guid warehouseId, string? search, CancellationToken ct = default)
    {
        var query = string.IsNullOrWhiteSpace(search) ? string.Empty : $"?search={Uri.EscapeDataString(search.Trim())}";
        return GetAsync<IReadOnlyList<ProductDto>>($"api/v1/pos/client/warehouses/{warehouseId:D}/products{query}", ct);
    }

    public Task<IReadOnlyList<CustomerDto>> SearchCustomersAsync(string? search = null, CancellationToken ct = default)
    {
        var query = string.IsNullOrWhiteSpace(search) ? string.Empty : $"?search={Uri.EscapeDataString(search.Trim())}";
        return GetAsync<IReadOnlyList<CustomerDto>>($"api/v1/customers{query}", ct);
    }

    public Task<CashSessionDto?> GetCashSessionAsync(string registerCode, CancellationToken ct = default) =>
        GetAsync<CashSessionDto?>($"api/v1/cash/session?registerCode={Uri.EscapeDataString(registerCode)}", ct);

    public Task<IReadOnlyList<PosOfflineDeviceDto>> GetOfflineDevicesAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PosOfflineDeviceDto>>("api/v1/pos/client/devices", ct);

    public async Task<PosOfflineDeviceDto> RegisterOfflineDeviceAsync(RegisterPosOfflineDeviceRequest request, CancellationToken ct = default) =>
        await PostAsync<PosOfflineDeviceDto>("api/v1/pos/client/devices", request, ct);

    public async Task RevokeOfflineDeviceAsync(Guid deviceId, long revision, CancellationToken ct = default)
    {
        using var response = await http.PostAsJsonAsync($"api/v1/pos/client/devices/{deviceId:D}/revoke", new RevokePosOfflineDeviceRequest(revision), ct);
        await EnsureSuccessAsync(response, ct);
    }

    public Task<IReadOnlyList<ParkedSaleDto>> GetParkedSalesAsync(string registerCode, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<ParkedSaleDto>>($"api/v1/pos/parked-sales/?registerCode={Uri.EscapeDataString(registerCode)}", ct);

    public async Task<ParkedSaleDto> ParkSaleAsync(ParkSaleRequest request, CancellationToken ct = default) =>
        await PostAsync<ParkedSaleDto>("api/v1/pos/parked-sales/", request, ct);

    public async Task<ParkedSaleDto> CancelParkedSaleAsync(Guid id, CancelParkedSaleRequest request, CancellationToken ct = default) =>
        await PostAsync<ParkedSaleDto>($"api/v1/pos/parked-sales/{id:D}/cancel", request, ct);

    public async Task<SaleDto> CheckoutAsync(CheckoutRequest request, CancellationToken ct = default)
    {
        return await PostAsync<SaleDto>("api/v1/pos/checkout", request, ct);
    }

    public async Task<SaleDto> CheckoutOfflineDraftAsync(OfflineCheckoutRequest request, CancellationToken ct = default) =>
        await PostAsync<SaleDto>("api/v1/pos/client/offline-checkout", request, ct);

    private async Task<T> PostAsync<T>(string path, object request, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync(path, request, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct)
            ?? throw new InvalidOperationException("El servidor no devolvió el resultado de la operación.");
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken ct)
    {
        using var response = await http.GetAsync(path, ct);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct)
            ?? throw new InvalidOperationException("El servidor devolvió una respuesta vacía.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new PosApiException("Tu sesión venció. Inicia sesión y vuelve a abrir el punto de venta.", response.StatusCode);
        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new PosApiException("Tu usuario no tiene permiso para operar este punto de venta.", response.StatusCode);

        var errors = await response.Content.ReadAsStringAsync(ct);
        throw new PosApiException(string.IsNullOrWhiteSpace(errors)
            ? "No se pudo completar la solicitud. Revisa los datos e inténtalo nuevamente."
            : errors, response.StatusCode);
    }
}

public sealed class PosApiException(string message, HttpStatusCode statusCode) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

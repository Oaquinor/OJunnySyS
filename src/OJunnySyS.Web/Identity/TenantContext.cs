using System.Security.Claims;
using OJunnySyS.Application.Abstractions;

namespace OJunnySyS.Web.Identity;

public sealed class TenantContext : ITenantContext
{
    public Guid? CurrentTenantId { get; private set; }
    public Guid? CurrentUserId { get; private set; }

    public void SetTenantId(Guid? tenantId)
    {
        if (tenantId == Guid.Empty) throw new InvalidOperationException("El tenant no puede ser un GUID vacío.");
        CurrentTenantId = tenantId;
    }

    public Guid GetRequiredTenantId() => CurrentTenantId
        ?? throw new InvalidOperationException("La cuenta no tiene una empresa activa. Inicia sesión de nuevo o solicita acceso al administrador de la plataforma.");

    public void SetFromPrincipal(ClaimsPrincipal principal)
    {
        var claim = principal.FindFirstValue("tenant_id");
        CurrentUserId = Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
        if (Guid.TryParse(claim, out var tenantId)) SetTenantId(tenantId);
        else SetTenantId(null);
    }
}

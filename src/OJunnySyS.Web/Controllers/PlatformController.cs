using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Application.Services;
using OJunnySyS.Domain.Common;

namespace OJunnySyS.Web.Controllers;

[Authorize(Roles = "PlatformAdministrator")]
[Route("platform")]
public sealed class PlatformController(PlatformManagementService service) : Controller
{
    [HttpGet("tenants")]
    public async Task<IActionResult> Tenants(CancellationToken ct) => View(await service.GetTenantsAsync(ct));

    [HttpPost("tenants")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTenant(CreateTenantInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View("Tenants", await service.GetTenantsAsync(ct));
        try
        {
            await service.CreateTenantAsync(new CreateTenantRequest(input.Name, input.Slug, input.OwnerEmail, input.TimeZoneId,
                input.CurrencyCode, input.AdministratorName, input.AdministratorEmail, input.InitialPassword), ct);
            TempData["Success"] = $"El espacio {input.Name} quedó creado. La persona administradora deberá cambiar su contraseña al entrar.";
            return RedirectToAction(nameof(Tenants));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewData["Input"] = input;
            return View("Tenants", await service.GetTenantsAsync(ct));
        }
    }

    [HttpPost("tenants/{tenantId:guid}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetTenantStatus(Guid tenantId, bool active, CancellationToken ct)
    {
        try
        {
            await service.SetTenantStatusAsync(tenantId, active, ct: ct);
            TempData["Success"] = active ? "Empresa activada." : "Empresa suspendida. Sus usuarios ya no pueden operar.";
        }
        catch (DomainException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Tenants));
    }
}

public sealed class CreateTenantInput
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(160)]
    public string Name { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public string Slug { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress]
    public string OwnerEmail { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(100)]
    public string TimeZoneId { get; set; } = "America/Santo_Domingo";
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.RegularExpression("^[A-Za-z]{3}$")]
    public string CurrencyCode { get; set; } = "DOP";
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(160)]
    public string AdministratorName { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress]
    public string AdministratorEmail { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(128, MinimumLength = 12)]
    public string InitialPassword { get; set; } = string.Empty;
}

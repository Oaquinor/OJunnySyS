using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Application.Services;
using OJunnySyS.Domain.Common;
using OJunnySyS.Infrastructure.Identity;

namespace OJunnySyS.Web.Controllers;

[Authorize(Roles = "PlatformAdministrator")]
[Route("platform")]
public sealed class PlatformController(
    PlatformManagementService service,
    UserManager<ApplicationUser> userManager,
    IAccountEmailSender emailSender,
    IWebHostEnvironment environment,
    ILogger<PlatformController> logger) : Controller
{
    [HttpGet("tenants")]
    public async Task<IActionResult> Tenants(CancellationToken ct) => View(await service.GetTenantsAsync(ct));

    [HttpPost("tenants")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTenant(CreateTenantInput input, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Input"] = input;
            return View("Tenants", await service.GetTenantsAsync(ct));
        }
        if (!emailSender.IsConfigured && !environment.IsDevelopment())
        {
            ModelState.AddModelError(string.Empty, "El alta desde plataforma requiere correo saliente para que la persona administradora confirme su cuenta.");
            ViewData["Input"] = input;
            return View("Tenants", await service.GetTenantsAsync(ct));
        }

        try
        {
            var tenant = await service.CreateTenantAsync(new CreateTenantRequest(input.Name, input.Slug, input.OwnerEmail, input.TimeZoneId,
                input.CurrencyCode, input.AdministratorName, input.AdministratorEmail, input.InitialPassword), ct);
            var administrator = await userManager.FindByEmailAsync(input.AdministratorEmail.Trim());
            if (administrator is null) throw new InvalidOperationException("Se creó la empresa, pero no se encontró su cuenta administradora para enviar la confirmación.");

            var token = await userManager.GenerateEmailConfirmationTokenAsync(administrator);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var confirmationUrl = Url.Action("ConfirmEmail", "Account", new { userId = administrator.Id, token = encodedToken }, Request.Scheme)!;
            var emailSent = false;
            if (emailSender.IsConfigured)
            {
                try
                {
                    await emailSender.SendConfirmationAsync(administrator.Email!, confirmationUrl, ct);
                    emailSent = true;
                }
                catch (Exception ex) when (ex is InvalidOperationException or System.Net.Mail.SmtpException or IOException)
                {
                    logger.LogError(ex, "No se pudo enviar el correo de confirmación de la empresa {TenantId}.", tenant.Id);
                }
            }

            TempData["Success"] = emailSent
                ? $"El espacio {input.Name} quedó creado. Se envió el enlace de confirmación a {administrator.Email}; al entrar deberá cambiar la contraseña temporal."
                : environment.IsDevelopment()
                    ? $"El espacio {input.Name} quedó creado. Confirma el correo administrador con el enlace local; luego podrá entrar y cambiar su contraseña temporal."
                    : $"El espacio {input.Name} quedó creado, pero no se pudo entregar el correo. Verifica el servicio de correo y vuelve a enviar la confirmación a {administrator.Email}.";
            if (!emailSent && environment.IsDevelopment()) TempData["ConfirmationUrl"] = confirmationUrl;
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

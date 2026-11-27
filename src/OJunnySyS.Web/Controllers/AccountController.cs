using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Common;
using OJunnySyS.Infrastructure.Identity;
using System.Text;

namespace OJunnySyS.Web.Controllers;

[Route("account")]
public sealed class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ITenantOnboardingService onboarding,
    IRegistrationEmailSender emailSender,
    IWebHostEnvironment environment,
    ILogger<AccountController> logger) : Controller
{
    [AllowAnonymous]
    [HttpGet("register")]
    public IActionResult Register() => User.Identity?.IsAuthenticated == true ? LocalRedirect("/app") : View(new RegisterInput());

    [AllowAnonymous]
    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("registration")]
    public async Task<IActionResult> Register(RegisterInput input, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true) return LocalRedirect("/app");
        if (!ModelState.IsValid) return View(input);
        if (!emailSender.IsConfigured && !environment.IsDevelopment())
        {
            ModelState.AddModelError(string.Empty, "El registro está temporalmente no disponible. Inténtalo más tarde.");
            return View(input);
        }

        try
        {
            var result = await onboarding.RegisterAsync(new TenantRegistrationRequest(
                input.CompanyName, input.OwnerName, input.Email, input.Password, input.TimeZoneId, input.CurrencyCode), cancellationToken);
            var token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(result.ConfirmationToken));
            var confirmationUrl = Url.Action(nameof(ConfirmEmail), "Account", new { userId = result.UserId, token }, Request.Scheme)!;
            var emailSent = false;
            if (emailSender.IsConfigured)
            {
                try
                {
                    await emailSender.SendConfirmationAsync(result.Email, confirmationUrl, cancellationToken);
                    emailSent = true;
                }
                catch (Exception ex) when (ex is InvalidOperationException or System.Net.Mail.SmtpException or IOException)
                {
                    logger.LogError(ex, "No se pudo enviar el correo de confirmación del nuevo espacio {TenantName}.", result.CompanyName);
                }
            }

            var localLink = environment.IsDevelopment() && !emailSent ? confirmationUrl : null;
            var message = emailSent
                ? "Revisa tu correo y confirma la dirección para activar el espacio de tu empresa."
                : localLink is not null
                    ? "El espacio está creado. En el entorno local, confirma tu correo con el enlace de desarrollo."
                    : "Si pudimos completar el registro, recibirás instrucciones para confirmar tu correo. Si no llega, solicita otro mensaje de confirmación.";
            return View("RegistrationSubmitted", new RegistrationSubmittedViewModel(result.CompanyName, localLink, message));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(input);
        }
    }

    [AllowAnonymous]
    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(Guid userId, string? token, CancellationToken cancellationToken)
    {
        var confirmed = false;
        if (userId != Guid.Empty && !string.IsNullOrWhiteSpace(token))
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
                confirmed = await onboarding.ConfirmEmailAsync(userId, decoded, cancellationToken);
            }
            catch (FormatException) { }
        }
        return View("EmailConfirmed", new EmailConfirmationViewModel(confirmed,
            confirmed ? "Tu correo está confirmado. Ya puedes iniciar sesión." : "El enlace no es válido, venció o ya no puede usarse. Solicita un nuevo mensaje de confirmación."));
    }

    [AllowAnonymous]
    [HttpGet("resend-confirmation")]
    public IActionResult ResendConfirmation() => View(new ResendConfirmationInput());

    [AllowAnonymous]
    [HttpPost("resend-confirmation")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("registration")]
    public async Task<IActionResult> ResendConfirmation(ResendConfirmationInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(input);
        if (emailSender.IsConfigured)
        {
            try
            {
                var rawToken = await onboarding.GenerateConfirmationTokenAsync(input.Email, cancellationToken);
                if (rawToken is not null)
                {
                    var user = await userManager.FindByEmailAsync(input.Email.Trim());
                    var token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
                    var link = Url.Action(nameof(ConfirmEmail), "Account", new { userId = user!.Id, token }, Request.Scheme)!;
                    await emailSender.SendConfirmationAsync(input.Email.Trim(), link, cancellationToken);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.Net.Mail.SmtpException or IOException)
            {
                logger.LogError(ex, "No se pudo reenviar el correo de confirmación.");
            }
        }
        return View("RegistrationSubmitted", new RegistrationSubmittedViewModel(string.Empty, null,
            "Si existe una cuenta pendiente para ese correo, recibirás un nuevo enlace de confirmación."));
    }

    [AllowAnonymous]
    [HttpGet("login")]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var current = await userManager.GetUserAsync(User);
            if (current?.MustChangePassword == true) return RedirectToAction(nameof(ChangePassword));
            return LocalRedirect(SafeReturnUrl(returnUrl));
        }
        return View(new LoginInput { ReturnUrl = SafeReturnUrl(returnUrl) });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginInput input, CancellationToken ct)
    {
        input.ReturnUrl = SafeReturnUrl(input.ReturnUrl);
        if (!ModelState.IsValid) return View(input);
        var result = await signInManager.PasswordSignInAsync(input.Email, input.Password, input.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            var user = await userManager.FindByEmailAsync(input.Email);
            if (user?.MustChangePassword == true) return RedirectToAction(nameof(ChangePassword));
            return LocalRedirect(input.ReturnUrl);
        }
        if (result.IsLockedOut) ModelState.AddModelError(string.Empty, "La cuenta está bloqueada temporalmente. Intenta más tarde.");
        else ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
        return View(input);
    }

    [Authorize]
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpGet("change-password")]
    public IActionResult ChangePassword() => View(new ChangePasswordInput());

    [HttpGet("tenant-unavailable")]
    public IActionResult TenantUnavailable() => View();

    [Authorize]
    [HttpPost("change-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordInput input)
    {
        if (!ModelState.IsValid) return View(input);
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        var result = await userManager.ChangePasswordAsync(user, input.CurrentPassword, input.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(input);
        }
        user.MustChangePassword = false;
        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            foreach (var error in update.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(input);
        }
        await signInManager.RefreshSignInAsync(user);
        return LocalRedirect("/app");
    }

    private string SafeReturnUrl(string? returnUrl) => !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
}

public sealed class LoginInput
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.EmailAddress]
    public string Email { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
    public string ReturnUrl { get; set; } = "/";
}

public sealed class ChangePasswordInput
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(128, MinimumLength = 12)]
    [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.Compare(nameof(NewPassword))]
    [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class RegisterInput
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(160, MinimumLength = 2)]
    public string CompanyName { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(160, MinimumLength = 2)]
    public string OwnerName { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress, System.ComponentModel.DataAnnotations.StringLength(254)]
    public string Email { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(128, MinimumLength = 12), System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
    public string Password { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.Compare(nameof(Password)), System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Required]
    public string TimeZoneId { get; set; } = "America/Santo_Domingo";
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.RegularExpression("^[A-Za-z]{3}$")]
    public string CurrencyCode { get; set; } = "DOP";
}

public sealed class ResendConfirmationInput
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress, System.ComponentModel.DataAnnotations.StringLength(254)]
    public string Email { get; set; } = string.Empty;
}

public sealed record EmailConfirmationViewModel(bool Confirmed, string Message);

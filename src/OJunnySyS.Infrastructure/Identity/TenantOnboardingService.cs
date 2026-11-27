using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Tenancy;
using OJunnySyS.Domain.Pos;
using OJunnySyS.Domain.Inventory;
using OJunnySyS.Infrastructure.Persistence;

namespace OJunnySyS.Infrastructure.Identity;

public sealed class TenantOnboardingService(
    StoreDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager) : ITenantOnboardingService
{
    public async Task<TenantRegistrationResult> RegisterAsync(TenantRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var companyName = request.CompanyName.Trim();
        var ownerName = request.OwnerName.Trim();
        var email = request.Email.Trim();
        if (companyName.Length is < 2 or > 160) throw new DomainException("El nombre de empresa debe tener entre 2 y 160 caracteres.");
        if (ownerName.Length is < 2 or > 160) throw new DomainException("El nombre del responsable debe tener entre 2 y 160 caracteres.");
        if (email.Length is < 3 or > 254 || !email.Contains('@', StringComparison.Ordinal)) throw new DomainException("El correo electrónico no es válido.");
        if (await userManager.FindByEmailAsync(email) is not null) throw new DomainException("No se pudo crear la cuenta con esos datos. Si ya te registraste, inicia sesión o solicita otro correo de confirmación.");
        if (!await roleManager.RoleExistsAsync(AppRoles.Administrator)) throw new InvalidOperationException("El rol de administrador de empresa no está inicializado.");

        var slug = await CreateAvailableSlugAsync(companyName, cancellationToken);
        var tenant = new Tenant(companyName, slug, email, request.TimeZoneId, request.CurrencyCode);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(cancellationToken);
        db.CurrentTenantId = tenant.Id;
        var warehouse = new Warehouse(tenant.Id, "MAIN", "Almacén principal");
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(cancellationToken);
        db.PosRegisters.Add(new PosRegister(tenant.Id, "MAIN", "Caja principal", warehouse.Id));
        await db.SaveChangesAsync(cancellationToken);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = false,
            DisplayName = ownerName,
            TenantId = tenant.Id,
            MustChangePassword = false,
            LockoutEnabled = true
        };
        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var duplicate = createResult.Errors.Any(x => x.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase));
            throw new DomainException(duplicate
                ? "No se pudo crear la cuenta con esos datos. Si ya te registraste, inicia sesión o solicita otro correo de confirmación."
                : string.Join(" ", createResult.Errors.Select(x => x.Description)));
        }

        var roleResult = await userManager.AddToRoleAsync(user, AppRoles.Administrator);
        if (!roleResult.Succeeded) throw new InvalidOperationException(string.Join(" ", roleResult.Errors.Select(x => x.Description)));
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await transaction.CommitAsync(cancellationToken);
        return new TenantRegistrationResult(user.Id, email, companyName, token);
    }

    public async Task<bool> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(token)) return false;
        var user = await userManager.FindByIdAsync(userId.ToString("D"));
        if (user is null || user.EmailConfirmed || user.TenantId is null) return user?.EmailConfirmed == true;
        var result = await userManager.ConfirmEmailAsync(user, token);
        cancellationToken.ThrowIfCancellationRequested();
        return result.Succeeded;
    }

    public async Task<string?> GenerateConfirmationTokenAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null || user.EmailConfirmed || user.TenantId is null) return null;
        cancellationToken.ThrowIfCancellationRequested();
        return await userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    private async Task<string> CreateAvailableSlugAsync(string name, CancellationToken cancellationToken)
    {
        var normalized = name.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(character));
        var baseSlug = System.Text.RegularExpressions.Regex.Replace(builder.ToString(), "[^a-z0-9]+", "-").Trim('-');
        if (baseSlug.Length < 2) baseSlug = "empresa";
        if (baseSlug.Length > 68) baseSlug = baseSlug[..68].TrimEnd('-');
        var slug = baseSlug;
        for (var suffix = 2; await db.Tenants.AnyAsync(x => x.Slug == slug, cancellationToken); suffix++)
        {
            var suffixText = $"-{suffix}";
            slug = $"{baseSlug[..Math.Min(baseSlug.Length, 80 - suffixText.Length)]}{suffixText}";
        }
        return slug;
    }
}

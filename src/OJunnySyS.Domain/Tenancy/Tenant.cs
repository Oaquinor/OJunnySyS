using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Tenancy;

public enum TenantStatus { Trial, Active, Suspended, Closed }

public sealed class Tenant : Entity
{
    private Tenant() { }

    public Tenant(string name, string slug, string ownerEmail, string timeZoneId, string currencyCode = "DOP")
    {
        UpdateProfile(name, slug, ownerEmail, timeZoneId, currencyCode);
        StartTrial(DateTimeOffset.UtcNow.AddDays(14));
    }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string OwnerEmail { get; private set; } = string.Empty;
    public string TimeZoneId { get; private set; } = "America/Santo_Domingo";
    public string CurrencyCode { get; private set; } = "DOP";
    public decimal DefaultTaxRate { get; private set; } = 0.18m;
    public string PlanCode { get; private set; } = "trial";
    public TenantStatus Status { get; private set; }
    public DateTimeOffset? TrialEndsAtUtc { get; private set; }

    public void UpdateProfile(string name, string slug, string ownerEmail, string timeZoneId, string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 160) throw new DomainException("El nombre de la empresa es obligatorio y no puede superar 160 caracteres.");
        if (string.IsNullOrWhiteSpace(slug) || slug.Trim().Length > 80 || !System.Text.RegularExpressions.Regex.IsMatch(slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$")) throw new DomainException("El identificador web debe usar letras minúsculas, números y guiones.");
        if (string.IsNullOrWhiteSpace(ownerEmail) || ownerEmail.Trim().Length > 254 || !ownerEmail.Contains('@')) throw new DomainException("El correo del responsable no es válido.");
        if (string.IsNullOrWhiteSpace(timeZoneId) || timeZoneId.Length > 100) throw new DomainException("La zona horaria no es válida.");
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length != 3) throw new DomainException("La moneda debe usar un código ISO de tres letras.");
        Name = name.Trim(); Slug = slug.Trim(); OwnerEmail = ownerEmail.Trim().ToLowerInvariant();
        TimeZoneId = timeZoneId.Trim(); CurrencyCode = currencyCode.Trim().ToUpperInvariant(); MarkUpdated();
    }

    public void Activate(string planCode)
    {
        if (Status == TenantStatus.Closed) throw new DomainException("Una empresa cerrada no se puede reactivar.");
        if (string.IsNullOrWhiteSpace(planCode) || planCode.Length > 40) throw new DomainException("Selecciona un plan válido.");
        PlanCode = planCode.Trim().ToLowerInvariant(); Status = TenantStatus.Active; TrialEndsAtUtc = null; MarkUpdated();
    }

    public void UpdateBusinessSettings(string timeZoneId, string currencyCode, decimal defaultTaxRate)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId) || timeZoneId.Length > 100) throw new DomainException("La zona horaria no es válida.");
        if (string.IsNullOrWhiteSpace(currencyCode) || !System.Text.RegularExpressions.Regex.IsMatch(currencyCode.Trim(), "^[A-Za-z]{3}$"))
            throw new DomainException("La moneda debe usar un código ISO de tres letras.");
        if (defaultTaxRate is < 0 or > 1) throw new DomainException("La tasa de impuesto debe estar entre 0 % y 100 %.");
        TimeZoneId = timeZoneId.Trim();
        CurrencyCode = currencyCode.Trim().ToUpperInvariant();
        DefaultTaxRate = decimal.Round(defaultTaxRate, 4);
        MarkUpdated();
    }

    public void Suspend() { if (Status == TenantStatus.Closed) throw new DomainException("La empresa ya está cerrada."); Status = TenantStatus.Suspended; MarkUpdated(); }
    public void Close() { Status = TenantStatus.Closed; MarkUpdated(); }
    public void StartTrial(DateTimeOffset endsAtUtc) { if (endsAtUtc <= DateTimeOffset.UtcNow) throw new DomainException("La fecha de fin de prueba debe estar en el futuro."); TrialEndsAtUtc = endsAtUtc.ToUniversalTime(); Status = TenantStatus.Trial; PlanCode = "trial"; MarkUpdated(); }

    public void EnsureCanOperate(DateTimeOffset nowUtc)
    {
        if (Status == TenantStatus.Suspended) throw new TenantAccessException("El acceso de esta empresa está suspendido. Contacta a la administración de OjunnySyS.");
        if (Status == TenantStatus.Closed) throw new TenantAccessException("Este espacio de empresa está cerrado.");
        if (Status == TenantStatus.Trial && TrialEndsAtUtc is { } end && end <= nowUtc)
            throw new TenantAccessException("El período de prueba de esta empresa terminó. Contacta a la administración para elegir un plan.");
    }
}

public sealed class TenantAccessException(string message) : Exception(message);

using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Reporting;

/// <summary>A tenant-owned, versioned layout used by printable business documents and reports.</summary>
public sealed class ReportTemplate : TenantEntity
{
    private ReportTemplate() { }

    public ReportTemplate(Guid tenantId, string code, string name, string reportType, string definitionJson) : base(tenantId)
    {
        Code = Normalize(code, 60, "El código de plantilla");
        Update(name, reportType, definitionJson, expectedRevision: null);
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string ReportType { get; private set; } = string.Empty;
    public string DefinitionJson { get; private set; } = "{}";
    public long Revision { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void Update(string name, string reportType, string definitionJson, long? expectedRevision)
    {
        if (expectedRevision is not null && expectedRevision != Revision)
            throw new ConcurrencyConflictException("El diseño cambió en otra sesión. Recarga la plantilla antes de guardar.");
        Name = Normalize(name, 120, "El nombre de plantilla");
        ReportType = Normalize(reportType, 40, "El tipo de reporte");
        if (string.IsNullOrWhiteSpace(definitionJson) || definitionJson.Length > 200_000)
            throw new DomainException("El diseño está vacío o supera el tamaño máximo permitido.");
        DefinitionJson = definitionJson;
        Revision++;
        MarkUpdated();
    }

    private static string Normalize(string value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
            throw new DomainException($"{label} es obligatorio y no puede superar {maxLength} caracteres.");
        return value.Trim();
    }
}

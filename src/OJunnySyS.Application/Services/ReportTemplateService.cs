using System.Text.Json;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Reporting;

namespace OJunnySyS.Application.Services;

public sealed class ReportTemplateService(IReportTemplateRepository repository)
{
    public async Task<IReadOnlyList<ReportTemplateDto>> GetAsync(CancellationToken ct = default) =>
        (await repository.GetAsync(ct)).Select(Map).ToArray();

    public async Task<ReportTemplateDto> CreateAsync(CreateReportTemplateRequest request, CancellationToken ct = default)
    {
        ValidateDefinition(request.DefinitionJson);
        return Map(await repository.CreateAsync(request, ct));
    }

    public async Task<ReportTemplateDto> UpdateAsync(Guid id, UpdateReportTemplateRequest request, CancellationToken ct = default)
    {
        ValidateDefinition(request.DefinitionJson);
        return Map(await repository.UpdateAsync(id, request, ct));
    }

    private static void ValidateDefinition(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new DomainException("El diseño debe ser un objeto JSON.");
        }
        catch (JsonException) { throw new DomainException("El diseño del reporte no contiene una estructura JSON válida."); }
    }

    private static ReportTemplateDto Map(ReportTemplate template) => new(template.Id, template.Code, template.Name, template.ReportType, template.DefinitionJson, template.Revision, template.IsActive, template.UpdatedAtUtc ?? template.CreatedAtUtc);
}

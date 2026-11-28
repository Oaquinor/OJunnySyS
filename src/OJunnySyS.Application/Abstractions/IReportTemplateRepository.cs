using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Reporting;

namespace OJunnySyS.Application.Abstractions;

public interface IReportTemplateRepository
{
    Task<IReadOnlyList<ReportTemplate>> GetAsync(CancellationToken cancellationToken = default);
    Task<ReportTemplate> CreateAsync(CreateReportTemplateRequest request, CancellationToken cancellationToken = default);
    Task<ReportTemplate> UpdateAsync(Guid id, UpdateReportTemplateRequest request, CancellationToken cancellationToken = default);
}

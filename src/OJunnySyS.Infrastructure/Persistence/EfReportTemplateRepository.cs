using Microsoft.EntityFrameworkCore;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Reporting;

namespace OJunnySyS.Infrastructure.Persistence;

public sealed class EfReportTemplateRepository(IDbContextFactory<StoreDbContext> contextFactory, ITenantContext tenantContext) : IReportTemplateRepository
{
    public async Task<IReadOnlyList<ReportTemplate>> GetAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.ReportTemplates.OrderBy(x => x.ReportType).ThenBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public async Task<ReportTemplate> CreateAsync(CreateReportTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var template = new ReportTemplate(tenantId, request.Code, request.Name, request.ReportType, request.DefinitionJson);
        if (await db.ReportTemplates.AnyAsync(x => x.Code == template.Code, cancellationToken))
            throw new DomainException("Ya existe una plantilla con ese código en esta empresa.");
        db.ReportTemplates.Add(template);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { throw new DomainException("No se pudo guardar la plantilla. Verifica que el código no esté duplicado."); }
        return template;
    }

    public async Task<ReportTemplate> UpdateAsync(Guid id, UpdateReportTemplateRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var template = await db.ReportTemplates.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new DomainException("La plantilla no existe en esta empresa.");
        template.Update(request.Name, request.ReportType, request.DefinitionJson, request.ExpectedRevision);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("El diseño cambió en otra sesión. Recarga la plantilla antes de guardar."); }
        return template;
    }
}

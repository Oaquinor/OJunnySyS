namespace OJunnySyS.Application.Contracts;

public sealed record ReportTemplateDto(Guid Id, string Code, string Name, string ReportType, string DefinitionJson, long Revision, bool IsActive, DateTimeOffset UpdatedAtUtc);
public sealed record CreateReportTemplateRequest(string Code, string Name, string ReportType, string DefinitionJson);
public sealed record UpdateReportTemplateRequest(string Name, string ReportType, string DefinitionJson, long ExpectedRevision);

namespace Respira.Analysis.Application.Contracts.Export;

/// <summary>A generated Excel report ready to be returned by the API.</summary>
public sealed record AnalysisReportExcelFile(
    Guid AnalysisId,
    Guid? DoctorId,
    string FileName,
    string ContentType,
    byte[] Content
);

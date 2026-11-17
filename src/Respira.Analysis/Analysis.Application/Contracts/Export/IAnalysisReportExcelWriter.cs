using Respira.Analysis.Application.Contracts;

namespace Respira.Analysis.Application.Contracts.Export;

/// <summary>Renders an analysis report as an Excel workbook.</summary>
public interface IAnalysisReportExcelWriter
{
    byte[] Write(AnalysisReportResult report);
}

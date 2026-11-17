using Microsoft.Extensions.DependencyInjection;
using Respira.Analysis.Application.Contracts.Export;
using Respira.Analysis.Infrastructure.Export;

namespace Respira.Analysis.Infrastructure.Extensions;

public static class DependencyInjection
{
    /// <summary>Registers the analysis report Excel exporter.</summary>
    public static IServiceCollection AddAnalysisExcelExport(this IServiceCollection services)
    {
        services.AddSingleton<IAnalysisReportExcelWriter, ClosedXmlAnalysisReportExcelWriter>();
        return services;
    }
}

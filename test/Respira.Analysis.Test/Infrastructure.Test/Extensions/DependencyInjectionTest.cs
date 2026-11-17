using Microsoft.Extensions.DependencyInjection;
using Respira.Analysis.Application.Contracts.Export;
using Respira.Analysis.Infrastructure.Export;
using Respira.Analysis.Infrastructure.Extensions;

namespace Respira.Analysis.Infrastructure.Test.Extensions;

public class DependencyInjectionTest
{
    [Fact]
    public void AddAnalysisExcelExport_RegistersClosedXmlWriterAsSingleton()
    {
        var services = new ServiceCollection();

        var returned = services.AddAnalysisExcelExport();

        Assert.Same(services, returned);

        var descriptor = Assert.Single(
            services,
            d => d.ServiceType == typeof(IAnalysisReportExcelWriter)
        );
        Assert.Equal(typeof(ClosedXmlAnalysisReportExcelWriter), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true }
        );
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var writer1 = scope1.ServiceProvider.GetRequiredService<IAnalysisReportExcelWriter>();
        var writer2 = scope2.ServiceProvider.GetRequiredService<IAnalysisReportExcelWriter>();

        Assert.IsType<ClosedXmlAnalysisReportExcelWriter>(writer1);
        Assert.Same(writer1, writer2);
    }
}

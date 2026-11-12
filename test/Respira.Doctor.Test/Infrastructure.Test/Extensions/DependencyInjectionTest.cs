using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Respira.Doctor.Application.Contracts.Data;
using Respira.Doctor.Application.Contracts.Enrichment;
using Respira.Doctor.Application.Contracts.Mappers;
using Respira.Doctor.Application.Features.Doctors.Common;
using Respira.Doctor.Infrastructure.Extensions;
using Respira.Doctor.Infrastructure.Mappers;
using Respira.Doctor.Infrastructure.Persistence;
using Wolverine;

namespace Respira.Doctor.Infrastructure.Test.Extensions;

public class DependencyInjectionTest
{
    [Fact]
    public void AddDoctorInfrastructure_RegistersExpectedServicesAndScopedLifetimes()
    {
        var services = new ServiceCollection();

        var returned = services.AddDoctorInfrastructure();

        Assert.Same(services, returned);

        var dbDescriptor = Assert.Single(services, d => d.ServiceType == typeof(IDoctorDbContext));
        Assert.Equal(ServiceLifetime.Scoped, dbDescriptor.Lifetime);

        var paginationDescriptor = Assert.Single(
            services,
            d => d.ServiceType == typeof(IPaginationFactory)
        );
        Assert.Equal(typeof(PaginationFactory), paginationDescriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, paginationDescriptor.Lifetime);

        var enricherDescriptor = Assert.Single(
            services,
            d => d.ServiceType == typeof(IDoctorProfileEnricher)
        );
        Assert.Equal(typeof(DoctorProfileEnricher), enricherDescriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, enricherDescriptor.Lifetime);
    }

    [Fact]
    public void AddDoctorInfrastructure_ResolvesAllServicesFromScope_AndForwardsDoctorDbContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => Mock.Of<IMessageBus>());
        services.AddDbContext<DoctorDbContext>(options =>
            options.UseNpgsql("Host=localhost;Database=doctor_di_test;")
        );
        services.AddDoctorInfrastructure();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true }
        );
        using var scope = provider.CreateScope();

        var concreteDb = scope.ServiceProvider.GetRequiredService<DoctorDbContext>();
        var abstractDb = scope.ServiceProvider.GetRequiredService<IDoctorDbContext>();
        var paginationFactory = scope.ServiceProvider.GetRequiredService<IPaginationFactory>();
        var enricher = scope.ServiceProvider.GetRequiredService<IDoctorProfileEnricher>();

        Assert.Same(concreteDb, abstractDb);
        Assert.IsType<PaginationFactory>(paginationFactory);
        Assert.IsType<DoctorProfileEnricher>(enricher);
    }
}

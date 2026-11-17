using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Respira.Saga.Application.Contracts.Media;
using Respira.Saga.Application.Contracts.Sagas;
using Respira.Saga.Infrastructure.Extensions;
using Respira.Saga.Infrastructure.Media;
using Respira.Saga.Infrastructure.Sagas;
using Wolverine.Persistence.Durability;
using Wolverine.RDBMS;

namespace Respira.Saga.Infrastructure.Test.Extensions;

public class DependencyInjectionTest
{
    [Fact]
    public void AddSagaInfrastructure_MissingMediaBaseUrl_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection([]).Build();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddSagaInfrastructure(configuration)
        );

        Assert.Equal("Configuration 'Media:BaseUrl' is not configured", exception.Message);
    }

    [Fact]
    public void AddSagaInfrastructure_RegistersMediaClientAndSagaStateReader()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["Media:BaseUrl"] = "https://media.respira.test" }
            )
            .Build();

        var messageDatabaseMock = new Mock<IMessageDatabase>();
        services.AddSingleton<IMessageStore>(messageDatabaseMock.Object);

        var returned = services.AddSagaInfrastructure(configuration);

        Assert.Same(services, returned);

        var readerDescriptor = Assert.Single(
            services,
            d => d.ServiceType == typeof(ISagaStateReader)
        );
        Assert.Equal(typeof(SagaStateReader), readerDescriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, readerDescriptor.Lifetime);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true }
        );
        using var scope = provider.CreateScope();

        var mediaClient = scope.ServiceProvider.GetRequiredService<IMediaClient>();
        var stateReader = scope.ServiceProvider.GetRequiredService<ISagaStateReader>();

        Assert.IsType<MediaClient>(mediaClient);
        Assert.IsType<SagaStateReader>(stateReader);
    }

    [Theory]
    [InlineData("https://media.respira.test", "https://media.respira.test/")]
    [InlineData("https://media.respira.test/", "https://media.respira.test/")]
    [InlineData("https://media.respira.test/prefix", "https://media.respira.test/prefix/")]
    public void AddSagaInfrastructure_NormalizesTrailingSlashOnMediaHttpClientBaseAddress(
        string configuredUrl,
        string expectedBaseAddress
    )
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["Media:BaseUrl"] = configuredUrl }
            )
            .Build();

        services.AddSagaInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        using var client = factory.CreateClient(nameof(IMediaClient));

        Assert.Equal(new Uri(expectedBaseAddress), client.BaseAddress);
    }
}

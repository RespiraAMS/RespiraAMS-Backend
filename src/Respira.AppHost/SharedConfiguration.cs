using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

namespace Respira.AppHost;

/// <summary>
/// Forwards the configuration shared by the services from the AppHost to a service.
/// The AppHost holds the settings in one place (src/Respira.AppHost/appsettings.json)
/// and hands them over as environment variables, so every service reads them from its
/// own configuration like any other value.
/// </summary>
internal static class SharedConfiguration
{
    /// <summary>
    /// Copies a section of the shared configuration into the resource as environment
    /// variables (e.g. the Email section becomes Email__Host and Email__Port).
    /// </summary>
    /// <param name="builder">The resource the configuration is forwarded to.</param>
    /// <param name="shared">The Shared section of the AppHost configuration.</param>
    /// <param name="sectionKey">Key of the section inside the Shared section.</param>
    /// <param name="targetPrefix">
    /// Configuration prefix the service expects. It defaults to <paramref name="sectionKey"/>,
    /// which is the name the section carries in the service configuration.
    /// </param>
    public static IResourceBuilder<T> WithSharedConfiguration<T>(
        this IResourceBuilder<T> builder,
        IConfiguration shared,
        string sectionKey,
        string? targetPrefix = null
    )
        where T : IResourceWithEnvironment
    {
        var prefix = targetPrefix ?? sectionKey;
        AddEnvironmentVariables(builder, shared.GetSection(sectionKey), prefix);
        return builder;
    }

    private static void AddEnvironmentVariables<T>(
        IResourceBuilder<T> builder,
        IConfiguration section,
        string prefix
    )
        where T : IResourceWithEnvironment
    {
        foreach (var child in section.GetChildren())
        {
            var key = $"{prefix}__{child.Key}";

            if (child.GetChildren().Any())
            {
                AddEnvironmentVariables(builder, child, key);
                continue;
            }

            builder.WithEnvironment(key, child.Value ?? string.Empty);
        }
    }
}

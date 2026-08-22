using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    /// <summary>
    /// Marker type used to anchor assembly scanning (FluentValidation, Wolverine discovery).
    /// It has no runtime behavior of its own.
    /// </summary>
    public class ApplicationMarker;

    /// <summary>
    /// Registers Application-layer services (validators) into the DI container.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Scans the Application assembly for FluentValidation validators and registers them.
        /// </summary>
        /// <param name="services">Service collection to register validators into</param>
        public static void AddFluentValidators(this IServiceCollection services)
        {
            services.AddValidatorsFromAssemblyContaining<ApplicationMarker>();
        }
    }
}

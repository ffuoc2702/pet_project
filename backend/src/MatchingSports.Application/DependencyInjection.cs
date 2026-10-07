using System.Reflection;
using MatchingSports.Application.Abstractions;
using MatchingSports.Application.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MatchingSports.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        string? autoMapperLicenseKey = null,
        params Assembly[] additionalMappingAssemblies)
    {
        services.TryAddScoped<IAuditActorContext, AuditActorContext>();
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            configuration.AddOpenBehavior(typeof(PermissionAuthorizationBehavior<,>));
            configuration.AddOpenBehavior(typeof(AuditActorBehavior<,>));
            configuration.AddOpenBehavior(typeof(PaginationValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(DateRangeValidationBehavior<,>));
        });

        var mappingAssemblies = new[] { typeof(ApplicationMappingProfile).Assembly }
            .Concat(additionalMappingAssemblies)
            .Distinct()
            .ToArray();

        services.AddAutoMapper(
            configuration =>
            {
                if (!string.IsNullOrWhiteSpace(autoMapperLicenseKey))
                    configuration.LicenseKey = autoMapperLicenseKey;
            },
            mappingAssemblies);

        return services;
    }
}

using MatchingSports.Application.Abstractions.Persistence;
using MatchingSports.Infrastructure.Persistence;
using MatchingSports.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MatchingSports.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("matchingsports")
            ?? throw new InvalidOperationException("Connection string 'matchingsports' is not configured.");

        services.AddDbContext<MatchingSportsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite()));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ISportRepository, SportRepository>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PersistenceExceptionBehavior<,>));

        return services;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.Modules.Events.Features.CreateVenue;
using VenuePass.Modules.Events.Infrastructure;

namespace VenuePass.Modules.Events;

public static class ModuleConfiguration
{
    public static IServiceCollection AddEventsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is missing.");

        services.AddDatabase(connectionString);
        services.RegisterHandlers();

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<EventsDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(EventsDbContext).Assembly.FullName);
                sql.MigrationsHistoryTable("__EFMigrationsHistory", EventsDbContext.Schema);
            });
        });

        return services;
    }

    private static IServiceCollection RegisterHandlers(this IServiceCollection services)
    {
        services.AddScoped<CreateVenueHandler>();
        return services;
    }
}
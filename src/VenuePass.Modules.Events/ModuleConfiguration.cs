using FluentValidation;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.Modules.Events.Features.CreateEvent;
using VenuePass.Modules.Events.Features.CreateManifestTemplate;
using VenuePass.Modules.Events.Features.CreateVenue;
using VenuePass.Modules.Events.Features.GetEvent;
using VenuePass.Modules.Events.Features.GetManifestTemplate;
using VenuePass.Modules.Events.Features.GetVenue;
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
        services.AddSingleton(TimeProvider.System);
        services.RegisterHandlers();
        services.AddValidatorsFromAssembly(typeof(ModuleConfiguration).Assembly);

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
        services.AddScoped<CreateEventHandler>();
        services.AddScoped<CreateManifestTemplateHandler>();
        services.AddScoped<CreateVenueHandler>();
        services.AddScoped<GetEventHandler>();
        services.AddScoped<GetManifestTemplateHandler>();
        services.AddScoped<GetVenueHandler>();
        return services;
    }
}
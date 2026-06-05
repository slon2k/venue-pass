using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.Modules.Ticketing.Infrastructure;

namespace VenuePass.Modules.Ticketing;

public static class ModuleConfiguration
{
    public static IServiceCollection AddTicketingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is missing.");

        services.AddDatabase(connectionString);
        services.AddSingleton(TimeProvider.System);
        services.RegisterHandlers();

        services.AddAuthorizationBuilder()
            .AddPolicy("EventAdmin", policy => policy.RequireRole("EventAdmin"))
            .AddPolicy("EventManager", policy => policy.RequireRole("EventManager"));

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<TicketingDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(TicketingDbContext).Assembly.FullName);
                sql.MigrationsHistoryTable("__EFMigrationsHistory", TicketingDbContext.Schema);
            });
        });

        return services;
    }

    private static IServiceCollection RegisterHandlers(this IServiceCollection services)
    {
        // Add scoped services for command and query handlers here, e.g.:
        // services.AddScoped<CreateTicketHandler>();
        return services;
    }
}
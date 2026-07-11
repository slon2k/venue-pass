using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

using VenuePass.Modules.Attendance.Infrastructure;
using VenuePass.Modules.Attendance.Infrastructure.Outbox;
using VenuePass.BuildingBlocks.Messaging;
using VenuePass.Modules.Ticketing.Contracts;
using VenuePass.Modules.Attendance.Features.TicketIssued;
using VenuePass.Modules.Attendance.Features.TicketCanceled;
using VenuePass.Modules.Attendance.Features.ScanTicket;

namespace VenuePass.Modules.Attendance;

public static class ModuleConfiguration
{
    public static IServiceCollection AddAttendanceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is missing.");

        services.AddDatabase(connectionString);
        services.RegisterHandlers();
        services.AddHostedService<AttendanceOutboxDispatcher>();

        services.AddAuthorizationBuilder()
            .AddPolicy("AttendanceAdmin", policy => policy.RequireRole("AttendanceAdmin"))
            .AddPolicy("AttendanceManager", policy => policy.RequireRole("AttendanceManager"))
            .AddPolicy("AttendanceOperator", policy => policy.RequireRole("AttendanceOperator"));

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AttendanceDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(AttendanceDbContext).Assembly.FullName);
                sql.MigrationsHistoryTable("__EFMigrationsHistory", AttendanceDbContext.Schema);
            });
        });

        return services;
    }

    private static IServiceCollection RegisterHandlers(this IServiceCollection services)
    {
        services.AddScoped<IIntegrationEventHandler<TicketIssuedIntegrationEvent>, TicketIssuedHandler>();
        services.AddScoped<IIntegrationEventHandler<TicketCanceledIntegrationEvent>, TicketCanceledHandler>();
        services.AddScoped<ScanTicketHandler>();

        return services;
    }
}
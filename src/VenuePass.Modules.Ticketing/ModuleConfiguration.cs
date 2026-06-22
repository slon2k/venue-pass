using FluentValidation;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using VenuePass.BuildingBlocks.Messaging;
using VenuePass.Modules.Events.Contracts.IntegrationEvents;
using VenuePass.Modules.Ticketing.Contracts;
using VenuePass.Modules.Ticketing.Domain.Tickets;
using VenuePass.Modules.Ticketing.Features.ActivateOffer;
using VenuePass.Modules.Ticketing.Features.CancelReservation;
using VenuePass.Modules.Ticketing.Features.CheckoutReservation;
using VenuePass.Modules.Ticketing.Features.ConfigurePricing;
using VenuePass.Modules.Ticketing.Features.CreateOffer;
using VenuePass.Modules.Ticketing.Features.CreateReservation;
using VenuePass.Modules.Ticketing.Features.EventPublished;
using VenuePass.Modules.Ticketing.Features.ExpireReservation;
using VenuePass.Modules.Ticketing.Features.GetInventoryStatus;
using VenuePass.Modules.Ticketing.Features.GetOffer;
using VenuePass.Modules.Ticketing.Features.GetOffers;
using VenuePass.Modules.Ticketing.Features.GetOrder;
using VenuePass.Modules.Ticketing.Features.GetReservation;
using VenuePass.Modules.Ticketing.Features.GetTicket;
using VenuePass.Modules.Ticketing.Infrastructure;
using VenuePass.Modules.Ticketing.Options;

namespace VenuePass.Modules.Ticketing;

public static class ModuleConfiguration
{
    public static IServiceCollection AddTicketingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is missing.");

        services.Configure<TicketingOptions>(configuration.GetSection(TicketingOptions.SectionName));
        services.AddDatabase(connectionString);
        services.AddSingleton(TimeProvider.System);
        services.RegisterHandlers();
        services.AddHostedService<ReservationExpirationWorker>();
        services.AddValidatorsFromAssembly(typeof(ModuleConfiguration).Assembly);        
        services.AddSingleton<TicketIssuer>();
        services.AddSingleton<ITicketCodeGenerator, TicketCodeGenerator>();
        services.AddScoped<ITicketingModuleContract, TicketModuleContract>();

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
        services.AddScoped<IIntegrationEventHandler<EventPublishedIntegrationEvent>, EventPublishedHandler>();
        services.AddScoped<CreateOfferHandler>();
        services.AddScoped<ConfigurePricingHandler>();
        services.AddScoped<ActivateOfferHandler>();
        services.AddScoped<GetOfferHandler>();
        services.AddScoped<GetOffersHandler>();
        services.AddScoped<GetInventoryStatusHandler>();
        services.AddScoped<CreateReservationHandler>();
        services.AddScoped<GetReservationHandler>();
        services.AddScoped<CancelReservationHandler>();
        services.AddScoped<ExpireReservationHandler>();
        services.AddScoped<CheckoutReservationHandler>();
        services.AddScoped<GetOrderHandler>();
        services.AddScoped<GetTicketHandler>();

        return services;
    }
}
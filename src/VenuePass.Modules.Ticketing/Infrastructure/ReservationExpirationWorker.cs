using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using VenuePass.BuildingBlocks.Application;
using VenuePass.Modules.Ticketing.Domain.Reservations;
using VenuePass.Modules.Ticketing.Features.ExpireReservation;
using VenuePass.Modules.Ticketing.Options;

namespace VenuePass.Modules.Ticketing.Infrastructure;

internal sealed class ReservationExpirationWorker(
    IServiceProvider serviceProvider,
    TimeProvider timeProvider,
    ILogger<ReservationExpirationWorker> logger,
    IOptions<TicketingOptions> options) : BackgroundService
{
    private readonly TimeSpan sweepInterval = options?.Value.ExpirationSweepInterval ?? throw new ArgumentException("Expiration sweep interval must be provided in options.", nameof(options));

    private readonly int batchSize = options?.Value.BatchSize > 0 ? options.Value.BatchSize : throw new ArgumentException("Expiration batch size must be greater than zero.", nameof(options));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpireReservations(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while expiring reservations.");
            }

            await Task.Delay(sweepInterval, stoppingToken);
        }
    }

    private async Task ExpireReservations(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();
        var handler = scope.ServiceProvider.GetRequiredService<ExpireReservationHandler>();

        var now = timeProvider.GetUtcNow();

        var reservationIds = await db.Reservations
            .AsNoTracking()
            .Where(r => r.Status == ReservationStatus.Reserved && r.ExpiresAt < now)
            .OrderBy(r => r.ExpiresAt)            
            .Select(r => r.Id.Value)
            .Take(batchSize)
            .ToListAsync(ct);

        foreach (var reservationId in reservationIds)
        {
            var result = await handler.Handle(new ExpireReservationCommand(reservationId), ct);

            result.Match(
                onSuccess: () => logger.LogDebug("Successfully expired reservation with ID {ReservationId}.", reservationId),
                onFailure: error =>
                {
                    switch (error.Type)
                    {
                    case ErrorType.Conflict:
                        logger.LogInformation("Could not expire reservation with ID {ReservationId} due to concurrency conflict. This may indicate a concurrent update (e.g. checkout) has already processed this reservation.", reservationId);
                        break;
                    default:
                        logger.LogWarning("An unexpected error occurred while expiring reservation with ID {ReservationId}: {Error}.", reservationId, error);
                        break;
                    }
                });
        }
    }
}
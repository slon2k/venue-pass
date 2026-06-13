using FluentValidation;
using FluentValidation.Results;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using VenuePass.BuildingBlocks.Application;
using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Events.Domain.ManifestTemplates;
using VenuePass.Modules.Events.Domain.Manifests;
using VenuePass.Modules.Events.Domain.Venues;
using VenuePass.Modules.Events.Infrastructure;

using DomainEventId = VenuePass.Modules.Events.Domain.Events.EventId;
using DomainEvent = VenuePass.Modules.Events.Domain.Events.Event;
using DomainEventName = VenuePass.Modules.Events.Domain.Events.EventName;
using DomainEventDescription = VenuePass.Modules.Events.Domain.Events.EventDescription;

namespace VenuePass.Modules.Events.Features.CreateEvent;

public sealed class CreateEventHandler(
    EventsDbContext db,
    IValidator<CreateEventCommand> validator,
    TimeProvider timeProvider,
    ILogger<CreateEventHandler> logger)
{
    public async Task<Result<CreateEventResult>> Handle(
        CreateEventCommand command,
        CancellationToken ct)
    {
        ValidationResult validationResult = await validator.ValidateAsync(command, ct);

        if (!validationResult.IsValid)
        {
            return CreateEventErrors.InvalidData(
                [.. validationResult.Errors.Select(e =>
                    new ValidationErrorDetail(e.PropertyName, e.ErrorMessage))]);
        }

        var venueId = new VenueId(command.VenueId);
        var templateId = new ManifestTemplateId(command.ManifestTemplateId);

        if (!await db.Venues.AnyAsync(v => v.Id == venueId, ct))
        {
            return CreateEventErrors.VenueNotFound(command.VenueId);
        }

        ManifestTemplate? template = await db.ManifestTemplates
            .Include(t => t.Sections)
            .ThenInclude(s => s.Rows)
            .ThenInclude(r => r.Seats)
            .Include(t => t.GeneralAdmissionAreas)
            .FirstOrDefaultAsync(t => t.Id == templateId, ct);

        if (template is null)
        {
            return CreateEventErrors.ManifestTemplateNotFound(command.ManifestTemplateId);
        }

        if (template.VenueId != venueId)
        {
            return CreateEventErrors.ManifestTemplateVenueMismatch(command.ManifestTemplateId, command.VenueId);
        }

        DomainEventId eventId = DomainEventId.Create();
        Manifest manifest;
        DomainEvent @event;

        try
        {
            manifest = Manifest.CreateFromTemplate(eventId, template);

            DomainEventDescription? description = command.Description is null
                ? null
                : new DomainEventDescription(command.Description);

            @event = DomainEvent.Create(
                eventId,
                venueId,
                manifest.Id,
                new DomainEventName(command.Name),
                command.EventDate,
                description,
                command.AssignedManagerId,
                timeProvider);
        }
        catch (DomainException ex)
        {
            logger.LogInformation(ex, "Domain exception occurred while creating event.");
            return Error.FromDomainException(ex);
        }
        catch (ArgumentException ex)
        {
            return CreateEventErrors.InvalidData(ex.Message);
        }

        db.Events.Add(@event);
        db.Manifests.Add(manifest);
        await db.SaveChangesAsync(ct);

        return new CreateEventResult(@event.Id, manifest.Id, @event.AssignedManagerId);
    }
}

public sealed record CreateEventCommand(
    Guid VenueId,
    Guid ManifestTemplateId,
    string Name,
    DateTimeOffset EventDate,
    string? Description,
    UserId AssignedManagerId);

public sealed record CreateEventResult(
    Guid EventId,
    Guid ManifestId,
    Guid AssignedManagerId);

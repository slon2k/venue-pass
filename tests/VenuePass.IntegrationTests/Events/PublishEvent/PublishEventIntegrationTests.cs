using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using VenuePass.BuildingBlocks.Messaging;
using VenuePass.Modules.Events.Contracts.IntegrationEvents;
using VenuePass.Modules.Events.Domain.Events;
using VenuePass.Modules.Events.Domain.Manifests;
using VenuePass.Modules.Events.Infrastructure;
using VenuePass.Modules.Events.Infrastructure.Outbox;
using VenuePass.IntegrationTests.Infrastructure;

using Xunit;

namespace VenuePass.IntegrationTests.Events.PublishEvent;

[Collection(EventsTestCollectionFixture.Name)]
public sealed class PublishEventEndpointIntegrationTests
{
    private readonly EventsIntegrationTestFixture _fixture;

    public PublishEventEndpointIntegrationTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Publish_WhenValidDraftEvent_ReturnsNoContentAndUpdatesState()
    {
        var managerId = Guid.NewGuid().ToString();
        using var managerClient = _fixture.CreateEventManagerClient(managerId);
        using var adminClient = _fixture.CreateAdminClient();

        var setup = await ArrangePublishableEventAsync(adminClient, managerClient);

        HttpResponseMessage response = await managerClient.PostAsync($"/events/{setup.EventId}/publish", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using IServiceScope scope = _fixture.Factory.Services.CreateScope();
        EventsDbContext db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        Event? persistedEvent = await db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == new EventId(setup.EventId));

        Assert.NotNull(persistedEvent);
        Assert.Equal(EventState.Published, persistedEvent!.State);
    }

    [Fact]
    public async Task Publish_WhenValidDraftEvent_FreezesManifest()
    {
        var managerId = Guid.NewGuid().ToString();
        using var managerClient = _fixture.CreateEventManagerClient(managerId);
        using var adminClient = _fixture.CreateAdminClient();

        var setup = await ArrangePublishableEventAsync(adminClient, managerClient);

        HttpResponseMessage response = await managerClient.PostAsync($"/events/{setup.EventId}/publish", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using IServiceScope scope = _fixture.Factory.Services.CreateScope();
        EventsDbContext db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        Manifest? manifest = await db.Manifests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == new ManifestId(setup.ManifestId));

        Assert.NotNull(manifest);
        Assert.True(manifest!.IsFrozen);
    }

    [Fact]
    public async Task Publish_WhenValidDraftEvent_WritesOutboxMessageWithExpectedPayload()
    {
        var managerId = Guid.NewGuid().ToString();
        using var managerClient = _fixture.CreateEventManagerClient(managerId);
        using var adminClient = _fixture.CreateAdminClient();

        var setup = await ArrangePublishableEventAsync(adminClient, managerClient);

        HttpResponseMessage response = await managerClient.PostAsync($"/events/{setup.EventId}/publish", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using IServiceScope scope = _fixture.Factory.Services.CreateScope();
        EventsDbContext db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        OutboxMessage message = await GetOutboxMessageForEventAsync(db, setup.EventId);

        Assert.Equal(typeof(EventPublishedIntegrationEvent).AssemblyQualifiedName, message.Type);

        EventPublishedIntegrationEvent? payload = JsonSerializer.Deserialize<EventPublishedIntegrationEvent>(message.Payload);
        Assert.NotNull(payload);
        Assert.Equal(setup.EventId, payload!.EventId);
        Assert.Equal(setup.ManifestId, payload.ManifestId);

        Assert.Null(message.ProcessedOn);
        Assert.True(message.NextAttemptOn <= message.OccurredOn);
    }

    [Fact]
    public async Task Publish_WhenEventAlreadyPublished_ReturnsConflictAndStateUnchanged()
    {
        var managerId = Guid.NewGuid().ToString();
        using var managerClient = _fixture.CreateEventManagerClient(managerId);
        using var adminClient = _fixture.CreateAdminClient();

        var setup = await ArrangePublishableEventAsync(adminClient, managerClient);

        HttpResponseMessage firstPublish = await managerClient.PostAsync($"/events/{setup.EventId}/publish", null);
        HttpResponseMessage secondPublish = await managerClient.PostAsync($"/events/{setup.EventId}/publish", null);

        Assert.Equal(HttpStatusCode.NoContent, firstPublish.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondPublish.StatusCode);

        using IServiceScope scope = _fixture.Factory.Services.CreateScope();
        EventsDbContext db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        Event? persistedEvent = await db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == new EventId(setup.EventId));

        Assert.NotNull(persistedEvent);
        Assert.Equal(EventState.Published, persistedEvent!.State);

        var outboxMessages = await db.OutboxMessages
            .AsNoTracking()
            .Where(x => x.Type == typeof(EventPublishedIntegrationEvent).AssemblyQualifiedName)
            .ToListAsync();

        int publishMessagesForEvent = outboxMessages.Count(x =>
        {
            return TryDeserializeEventPublishedPayload(x.Payload, out var payload)
                && payload?.EventId == setup.EventId;
        });

        Assert.Equal(1, publishMessagesForEvent);
    }

    [Fact]
    public async Task Publish_WhenEventDateInPast_ReturnsBadRequestAndStateUnchanged()
    {
        var managerId = Guid.NewGuid().ToString();
        using var managerClient = _fixture.CreateEventManagerClient(managerId);
        using var adminClient = _fixture.CreateAdminClient();

        var setup = await ArrangePublishableEventAsync(adminClient, managerClient);

        await using (var arrangeScope = _fixture.Factory.Services.CreateAsyncScope())
        {
            EventsDbContext arrangeDb = arrangeScope.ServiceProvider.GetRequiredService<EventsDbContext>();
            DateTimeOffset pastDate = DateTimeOffset.UtcNow.AddMinutes(-5);

            await arrangeDb.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE [events].[events]
                SET [event_date] = {pastDate}
                WHERE [id] = {setup.EventId}
                """);
        }

        HttpResponseMessage response = await managerClient.PostAsync($"/events/{setup.EventId}/publish", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using IServiceScope scope = _fixture.Factory.Services.CreateScope();
        EventsDbContext db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        Event? persistedEvent = await db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == new EventId(setup.EventId));

        Assert.NotNull(persistedEvent);
        Assert.Equal(EventState.Draft, persistedEvent!.State);
    }

    [Fact]
    public async Task Publish_WhenCallerIsNotAssignedManager_ReturnsForbiddenAndStateUnchanged()
    {
        var assignedManagerId = Guid.NewGuid().ToString();
        var anotherManagerId = Guid.NewGuid().ToString();

        using var assignedManagerClient = _fixture.CreateEventManagerClient(assignedManagerId);
        using var anotherManagerClient = _fixture.CreateEventManagerClient(anotherManagerId);
        using var adminClient = _fixture.CreateAdminClient();

        var setup = await ArrangePublishableEventAsync(adminClient, assignedManagerClient);

        HttpResponseMessage response = await anotherManagerClient.PostAsync($"/events/{setup.EventId}/publish", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using IServiceScope scope = _fixture.Factory.Services.CreateScope();
        EventsDbContext db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

        Event? persistedEvent = await db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == new EventId(setup.EventId));

        Assert.NotNull(persistedEvent);
        Assert.Equal(EventState.Draft, persistedEvent!.State);
    }

    [Fact]
    public async Task Publish_WhenNoAuthToken_ReturnsUnauthorized()
    {
        HttpClient unauthenticated = _fixture.Client;

        HttpResponseMessage response = await unauthenticated.PostAsync($"/events/{Guid.NewGuid()}/publish", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Publish_WhenWrongRole_ReturnsForbidden()
    {
        using var adminClient = _fixture.CreateAdminClient();

        HttpResponseMessage response = await adminClient.PostAsync($"/events/{Guid.NewGuid()}/publish", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<OutboxMessage> GetOutboxMessageForEventAsync(EventsDbContext db, Guid eventId)
    {
        List<OutboxMessage> candidates = await db.OutboxMessages
            .AsNoTracking()
            .Where(x => x.Type == typeof(EventPublishedIntegrationEvent).AssemblyQualifiedName)
            .OrderByDescending(x => x.OccurredOn)
            .ToListAsync();

        OutboxMessage? match = candidates.FirstOrDefault(x =>
        {
            return TryDeserializeEventPublishedPayload(x.Payload, out var payload)
                && payload?.EventId == eventId;
        });

        return match ?? throw new Xunit.Sdk.XunitException($"Outbox message for event '{eventId}' was not found.");
    }

    private static async Task<(Guid EventId, Guid ManifestId)> ArrangePublishableEventAsync(
        HttpClient adminClient,
        HttpClient managerClient)
    {
        Guid venueId = await CreateVenueAsync(adminClient);
        Guid templateId = await CreateManifestTemplateAsync(adminClient, venueId);

        CreateEventResponse created = await CreateEventAsync(managerClient, venueId, templateId);
        return (created.EventId, created.ManifestId);
    }

    private static async Task<Guid> CreateVenueAsync(HttpClient client)
    {
        CreateVenueRequest request = new(
            Name: $"Venue {Guid.NewGuid()}",
            Address: "123 Main St",
            City: "Seattle",
            Country: "US",
            Capacity: 500);

        HttpResponseMessage response = await client.PostAsJsonAsync("/events/venues", request);
        CreateVenueResponse? body = await response.Content.ReadFromJsonAsync<CreateVenueResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        return body!.VenueId;
    }

    private static async Task<Guid> CreateManifestTemplateAsync(HttpClient client, Guid venueId)
    {
        CreateManifestTemplateRequest request = new(
            Name: $"Template {Guid.NewGuid()}",
            Description: "Concert layout",
            VenueId: venueId,
            Sections:
            [
                new CreateManifestTemplateSectionRequest(
                    Name: "Floor",
                    Rows:
                    [
                        new CreateManifestTemplateRowRequest(
                            Label: "A",
                            Seats:
                            [
                                new CreateManifestTemplateSeatRequest("1"),
                                new CreateManifestTemplateSeatRequest("2")
                            ])
                    ])
            ],
            GeneralAdmissionAreas:
            [
                new CreateManifestTemplateGeneralAdmissionAreaRequest(
                    Name: "GA East",
                    Capacity: 300)
            ]);

        HttpResponseMessage response = await client.PostAsJsonAsync("/events/manifest-templates", request);
        CreateManifestTemplateResponse? body = await response.Content.ReadFromJsonAsync<CreateManifestTemplateResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        return body!.ManifestTemplateId;
    }

    private static async Task<CreateEventResponse> CreateEventAsync(HttpClient client, Guid venueId, Guid templateId)
    {
        CreateEventRequest request = new(
            VenueId: venueId,
            ManifestTemplateId: templateId,
            Name: $"Event {Guid.NewGuid()}",
            EventDate: DateTimeOffset.UtcNow.AddMonths(2),
            Description: "Publish integration test event");

        HttpResponseMessage response = await client.PostAsJsonAsync("/events", request);
        CreateEventResponse? body = await response.Content.ReadFromJsonAsync<CreateEventResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        return body!;
    }

    private static bool TryDeserializeEventPublishedPayload(
        string payload,
        out EventPublishedIntegrationEvent? integrationEvent)
    {
        try
        {
            integrationEvent = JsonSerializer.Deserialize<EventPublishedIntegrationEvent>(payload);
            return integrationEvent is not null;
        }
        catch (JsonException)
        {
            integrationEvent = null;
            return false;
        }
    }

    private sealed record CreateEventRequest(
        Guid VenueId,
        Guid ManifestTemplateId,
        string Name,
        DateTimeOffset EventDate,
        string? Description);

    private sealed record CreateEventResponse(Guid EventId, Guid ManifestId, Guid AssignedManagerId);

    private sealed record CreateVenueRequest(
        string Name,
        string Address,
        string City,
        string Country,
        int Capacity);

    private sealed record CreateVenueResponse(
        Guid VenueId,
        string Name,
        string Address,
        string City,
        string Country,
        int Capacity);

    private sealed record CreateManifestTemplateRequest(
        string Name,
        string? Description,
        Guid VenueId,
        IReadOnlyList<CreateManifestTemplateSectionRequest> Sections,
        IReadOnlyList<CreateManifestTemplateGeneralAdmissionAreaRequest> GeneralAdmissionAreas);

    private sealed record CreateManifestTemplateSectionRequest(
        string Name,
        IReadOnlyList<CreateManifestTemplateRowRequest> Rows);

    private sealed record CreateManifestTemplateRowRequest(
        string Label,
        IReadOnlyList<CreateManifestTemplateSeatRequest> Seats);

    private sealed record CreateManifestTemplateSeatRequest(string Label);

    private sealed record CreateManifestTemplateGeneralAdmissionAreaRequest(string Name, int Capacity);

    private sealed record CreateManifestTemplateResponse(Guid ManifestTemplateId);
}

[Collection(EventsTestCollectionFixture.Name)]
public sealed class OutboxDispatcherIntegrationTests
{
    private readonly EventsIntegrationTestFixture _fixture;

    public OutboxDispatcherIntegrationTests(EventsIntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Dispatcher_WhenOutboxMessageExists_ProcessesAndHandlerReceivesPayload()
    {
        var recorder = new PublishEventHandlerRecorder();

        await using EventsApiFactory factory = _fixture.CreateFactory(
            enableOutboxDispatcher: true,
            configureTestServices: services =>
            {
                services.AddSingleton(recorder);
                services.AddScoped<IIntegrationEventHandler<EventPublishedIntegrationEvent>, RecordingPublishEventHandler>();
            });

        using HttpClient adminClient = factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, Guid.NewGuid().ToString());
        adminClient.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "EventAdmin");

        var managerId = Guid.NewGuid().ToString();
        using HttpClient managerClient = factory.CreateClient();
        managerClient.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, managerId);
        managerClient.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "EventManager");

        var setup = await ArrangePublishableEventAsync(adminClient, managerClient);

        HttpResponseMessage publishResponse = await managerClient.PostAsync($"/events/{setup.EventId}/publish", null);
        Assert.Equal(HttpStatusCode.NoContent, publishResponse.StatusCode);

        await WaitUntilAsync(async () =>
        {
            await using var scope = factory.Services.CreateAsyncScope();
            EventsDbContext db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

            OutboxMessage message = await GetOutboxMessageForEventAsync(db, setup.EventId);
            return message.ProcessedOn is not null;
        }, timeout: TimeSpan.FromSeconds(12));

        Assert.Contains(recorder.HandledEvents, e => e.EventId == setup.EventId && e.ManifestId == setup.ManifestId);
    }

    [Fact]
    public async Task Dispatcher_WhenHandlerFails_DoesNotMarkProcessedAndRecordsRetryMetadata()
    {
        await using EventsApiFactory factory = _fixture.CreateFactory(
            enableOutboxDispatcher: true,
            configureTestServices: services =>
                services.AddScoped<IIntegrationEventHandler<EventPublishedIntegrationEvent>, FailingPublishEventHandler>());

        using HttpClient adminClient = factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, Guid.NewGuid().ToString());
        adminClient.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "EventAdmin");

        var managerId = Guid.NewGuid().ToString();
        using HttpClient managerClient = factory.CreateClient();
        managerClient.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, managerId);
        managerClient.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "EventManager");

        var setup = await ArrangePublishableEventAsync(adminClient, managerClient);

        HttpResponseMessage publishResponse = await managerClient.PostAsync($"/events/{setup.EventId}/publish", null);
        Assert.Equal(HttpStatusCode.NoContent, publishResponse.StatusCode);

        await WaitUntilAsync(async () =>
        {
            await using var scope = factory.Services.CreateAsyncScope();
            EventsDbContext db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

            OutboxMessage message = await GetOutboxMessageForEventAsync(db, setup.EventId);
            return message.AttemptCount >= 1;
        }, timeout: TimeSpan.FromSeconds(12));

        await using var assertScope = factory.Services.CreateAsyncScope();
        EventsDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<EventsDbContext>();

        OutboxMessage persisted = await GetOutboxMessageForEventAsync(assertDb, setup.EventId);

        Assert.Null(persisted.ProcessedOn);
        Assert.True(persisted.AttemptCount >= 1);
        Assert.NotNull(persisted.LastAttemptedOn);
        Assert.NotNull(persisted.NextAttemptOn);
        Assert.NotNull(persisted.Error);
        Assert.Contains(FailingPublishEventHandler.ErrorMessage, persisted.Error);
    }

    [Fact]
    public async Task Dispatcher_WhenNoHandlerRegistered_MarksMessageProcessed()
    {
        await using EventsApiFactory factory = _fixture.CreateFactory(
            enableOutboxDispatcher: true,
            configureTestServices: services =>
                services.RemoveAll<IIntegrationEventHandler<EventPublishedIntegrationEvent>>());

        using HttpClient adminClient = factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, Guid.NewGuid().ToString());
        adminClient.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "EventAdmin");

        var managerId = Guid.NewGuid().ToString();
        using HttpClient managerClient = factory.CreateClient();
        managerClient.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, managerId);
        managerClient.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "EventManager");

        var setup = await ArrangePublishableEventAsync(adminClient, managerClient);

        HttpResponseMessage publishResponse = await managerClient.PostAsync($"/events/{setup.EventId}/publish", null);
        Assert.Equal(HttpStatusCode.NoContent, publishResponse.StatusCode);

        await WaitUntilAsync(async () =>
        {
            await using var scope = factory.Services.CreateAsyncScope();
            EventsDbContext db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

            OutboxMessage message = await GetOutboxMessageForEventAsync(db, setup.EventId);
            return message.ProcessedOn is not null;
        }, timeout: TimeSpan.FromSeconds(12));

        await using var assertScope = factory.Services.CreateAsyncScope();
        EventsDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<EventsDbContext>();

        OutboxMessage persisted = await GetOutboxMessageForEventAsync(assertDb, setup.EventId);
        Assert.NotNull(persisted.ProcessedOn);
    }

    [Fact]
    public async Task Dispatcher_WhenMessageTypeIsUnresolvable_MarksMessageProcessed()
    {
        await using EventsApiFactory factory = _fixture.CreateFactory(enableOutboxDispatcher: true);

        Guid messageId;
        await using (var seedScope = factory.Services.CreateAsyncScope())
        {
            EventsDbContext db = seedScope.ServiceProvider.GetRequiredService<EventsDbContext>();
            var message = OutboxMessage.Create(
                occurredOn: DateTimeOffset.UtcNow,
                type: "Missing.Type.Name, Missing.Assembly",
                payload: "{\"x\":1}");

            db.OutboxMessages.Add(message);
            await db.SaveChangesAsync();
            messageId = message.Id;
        }

        await WaitUntilAsync(async () =>
        {
            await using var scope = factory.Services.CreateAsyncScope();
            EventsDbContext db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
            OutboxMessage? message = await db.OutboxMessages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == messageId);
            return message is not null && message.ProcessedOn is not null;
        }, timeout: TimeSpan.FromSeconds(12));
    }

    [Fact]
    public async Task Dispatcher_WhenMessagePayloadIsInvalid_MarksMessageProcessed()
    {
        await using EventsApiFactory factory = _fixture.CreateFactory(enableOutboxDispatcher: true);

        Guid messageId;
        await using (var seedScope = factory.Services.CreateAsyncScope())
        {
            EventsDbContext db = seedScope.ServiceProvider.GetRequiredService<EventsDbContext>();
            var message = OutboxMessage.Create(
                occurredOn: DateTimeOffset.UtcNow,
                type: typeof(EventPublishedIntegrationEvent).AssemblyQualifiedName!,
                payload: "not-json");

            db.OutboxMessages.Add(message);
            await db.SaveChangesAsync();
            messageId = message.Id;
        }

        await WaitUntilAsync(async () =>
        {
            await using var scope = factory.Services.CreateAsyncScope();
            EventsDbContext db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
            OutboxMessage? message = await db.OutboxMessages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == messageId);
            return message is not null && message.ProcessedOn is not null;
        }, timeout: TimeSpan.FromSeconds(12));
    }

    [Fact]
    public async Task Dispatcher_WhenMessageIsNotYetEligible_SkipsMessage()
    {
        await using EventsApiFactory factory = _fixture.CreateFactory(enableOutboxDispatcher: true);

        Guid messageId;
        DateTimeOffset expectedNextAttempt;

        await using (var seedScope = factory.Services.CreateAsyncScope())
        {
            EventsDbContext db = seedScope.ServiceProvider.GetRequiredService<EventsDbContext>();
            var message = OutboxMessage.Create(
                occurredOn: DateTimeOffset.UtcNow,
                type: typeof(EventPublishedIntegrationEvent).AssemblyQualifiedName!,
                payload: JsonSerializer.Serialize(new EventPublishedIntegrationEvent(
                    MessageId: Guid.CreateVersion7(),
                    EventId: Guid.CreateVersion7(),
                    ManifestId: Guid.CreateVersion7(),
                    OccurredOn: DateTimeOffset.UtcNow)));

            expectedNextAttempt = DateTimeOffset.UtcNow.AddMinutes(2);
            message.RecordFailure(DateTimeOffset.UtcNow, expectedNextAttempt, "seeded failure");

            db.OutboxMessages.Add(message);
            await db.SaveChangesAsync();
            messageId = message.Id;
        }

        await Task.Delay(TimeSpan.FromSeconds(6));

        await using var assertScope = factory.Services.CreateAsyncScope();
        EventsDbContext assertDb = assertScope.ServiceProvider.GetRequiredService<EventsDbContext>();
        OutboxMessage? persisted = await assertDb.OutboxMessages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == messageId);

        Assert.NotNull(persisted);
        Assert.Null(persisted!.ProcessedOn);
        Assert.Equal(1, persisted.AttemptCount);
        Assert.Equal(expectedNextAttempt, persisted.NextAttemptOn);
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> predicate, TimeSpan timeout)
    {
        var start = DateTimeOffset.UtcNow;

        while (DateTimeOffset.UtcNow - start < timeout)
        {
            if (await predicate())
            {
                return;
            }

            await Task.Delay(200);
        }

        throw new Xunit.Sdk.XunitException($"Condition was not satisfied within {timeout.TotalSeconds} seconds.");
    }

    private static async Task<OutboxMessage> GetOutboxMessageForEventAsync(EventsDbContext db, Guid eventId)
    {
        List<OutboxMessage> candidates = await db.OutboxMessages
            .AsNoTracking()
            .Where(x => x.Type == typeof(EventPublishedIntegrationEvent).AssemblyQualifiedName)
            .OrderByDescending(x => x.OccurredOn)
            .ToListAsync();

        OutboxMessage? match = candidates.FirstOrDefault(x =>
        {
            return TryDeserializeEventPublishedPayload(x.Payload, out var payload)
                && payload?.EventId == eventId;
        });

        return match ?? throw new Xunit.Sdk.XunitException($"Outbox message for event '{eventId}' was not found.");
    }

    private static async Task<(Guid EventId, Guid ManifestId)> ArrangePublishableEventAsync(
        HttpClient adminClient,
        HttpClient managerClient)
    {
        Guid venueId = await CreateVenueAsync(adminClient);
        Guid templateId = await CreateManifestTemplateAsync(adminClient, venueId);

        CreateEventResponse created = await CreateEventAsync(managerClient, venueId, templateId);
        return (created.EventId, created.ManifestId);
    }

    private static async Task<Guid> CreateVenueAsync(HttpClient client)
    {
        CreateVenueRequest request = new(
            Name: $"Venue {Guid.NewGuid()}",
            Address: "123 Main St",
            City: "Seattle",
            Country: "US",
            Capacity: 500);

        HttpResponseMessage response = await client.PostAsJsonAsync("/events/venues", request);
        CreateVenueResponse? body = await response.Content.ReadFromJsonAsync<CreateVenueResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        return body!.VenueId;
    }

    private static async Task<Guid> CreateManifestTemplateAsync(HttpClient client, Guid venueId)
    {
        CreateManifestTemplateRequest request = new(
            Name: $"Template {Guid.NewGuid()}",
            Description: "Concert layout",
            VenueId: venueId,
            Sections:
            [
                new CreateManifestTemplateSectionRequest(
                    Name: "Floor",
                    Rows:
                    [
                        new CreateManifestTemplateRowRequest(
                            Label: "A",
                            Seats:
                            [
                                new CreateManifestTemplateSeatRequest("1"),
                                new CreateManifestTemplateSeatRequest("2")
                            ])
                    ])
            ],
            GeneralAdmissionAreas:
            [
                new CreateManifestTemplateGeneralAdmissionAreaRequest(
                    Name: "GA East",
                    Capacity: 300)
            ]);

        HttpResponseMessage response = await client.PostAsJsonAsync("/events/manifest-templates", request);
        CreateManifestTemplateResponse? body = await response.Content.ReadFromJsonAsync<CreateManifestTemplateResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        return body!.ManifestTemplateId;
    }

    private static async Task<CreateEventResponse> CreateEventAsync(HttpClient client, Guid venueId, Guid templateId)
    {
        CreateEventRequest request = new(
            VenueId: venueId,
            ManifestTemplateId: templateId,
            Name: $"Event {Guid.NewGuid()}",
            EventDate: DateTimeOffset.UtcNow.AddMonths(2),
            Description: "Publish integration test event");

        HttpResponseMessage response = await client.PostAsJsonAsync("/events", request);
        CreateEventResponse? body = await response.Content.ReadFromJsonAsync<CreateEventResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        return body!;
    }

    private sealed record CreateEventRequest(
        Guid VenueId,
        Guid ManifestTemplateId,
        string Name,
        DateTimeOffset EventDate,
        string? Description);

    private sealed record CreateEventResponse(Guid EventId, Guid ManifestId, Guid AssignedManagerId);

    private sealed record CreateVenueRequest(
        string Name,
        string Address,
        string City,
        string Country,
        int Capacity);

    private sealed record CreateVenueResponse(
        Guid VenueId,
        string Name,
        string Address,
        string City,
        string Country,
        int Capacity);

    private sealed record CreateManifestTemplateRequest(
        string Name,
        string? Description,
        Guid VenueId,
        IReadOnlyList<CreateManifestTemplateSectionRequest> Sections,
        IReadOnlyList<CreateManifestTemplateGeneralAdmissionAreaRequest> GeneralAdmissionAreas);

    private sealed record CreateManifestTemplateSectionRequest(
        string Name,
        IReadOnlyList<CreateManifestTemplateRowRequest> Rows);

    private sealed record CreateManifestTemplateRowRequest(
        string Label,
        IReadOnlyList<CreateManifestTemplateSeatRequest> Seats);

    private sealed record CreateManifestTemplateSeatRequest(string Label);

    private sealed record CreateManifestTemplateGeneralAdmissionAreaRequest(string Name, int Capacity);

    private sealed record CreateManifestTemplateResponse(Guid ManifestTemplateId);

    private sealed class PublishEventHandlerRecorder
    {
        public List<EventPublishedIntegrationEvent> HandledEvents { get; } = [];
    }

    private sealed class RecordingPublishEventHandler(PublishEventHandlerRecorder recorder)
        : IIntegrationEventHandler<EventPublishedIntegrationEvent>
    {
        public Task Handle(EventPublishedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            recorder.HandledEvents.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class FailingPublishEventHandler : IIntegrationEventHandler<EventPublishedIntegrationEvent>
    {
        public const string ErrorMessage = "integration dispatcher handler failure";

        public Task Handle(EventPublishedIntegrationEvent integrationEvent, CancellationToken cancellationToken)
            => throw new InvalidOperationException(ErrorMessage);
    }

    private static bool TryDeserializeEventPublishedPayload(
        string payload,
        out EventPublishedIntegrationEvent? integrationEvent)
    {
        try
        {
            integrationEvent = JsonSerializer.Deserialize<EventPublishedIntegrationEvent>(payload);
            return integrationEvent is not null;
        }
        catch (JsonException)
        {
            integrationEvent = null;
            return false;
        }
    }
}

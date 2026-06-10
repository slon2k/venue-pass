using Xunit;

namespace VenuePass.Modules.Events.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class EventsTestCollectionFixture : ICollectionFixture<EventsIntegrationTestFixture>
{
    public const string Name = "Events";
}

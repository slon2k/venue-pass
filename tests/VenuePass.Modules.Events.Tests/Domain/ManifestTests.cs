using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Events.Domain.Events;
using VenuePass.Modules.Events.Domain.ManifestTemplates;
using VenuePass.Modules.Events.Domain.Manifests;
using VenuePass.Modules.Events.Domain.Venues;
using Xunit;

namespace VenuePass.Modules.Events.Tests.Domain;

public sealed class ManifestTests
{
    // ── CreateFromTemplate ────────────────────────────────────────────────────

    [Fact]
    public void CreateFromTemplate_WithNullTemplate_ThrowsArgumentNullException()
    {
        void Act() => Manifest.CreateFromTemplate(EventId.Create(), null!);

        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void CreateFromTemplate_WithSections_CopiesSectionsToManifest()
    {
        var template = BuildTemplate(
            sections: [new SectionDraft("Main Floor", [new RowDraft("A", [new SeatDraft("1"), new SeatDraft("2")])])],
            gaAreas: []);

        var manifest = Manifest.CreateFromTemplate(EventId.Create(), template);

        var section = Assert.Single(manifest.Sections);
        Assert.Equal("Main Floor", section.Name.Value);
        var row = Assert.Single(section.Rows);
        Assert.Equal("A", row.Label.Value);
        Assert.Equal(2, row.Seats.Count);
    }

    [Fact]
    public void CreateFromTemplate_WithGeneralAdmissionAreas_CopiesAreasToManifest()
    {
        var template = BuildTemplate(
            sections: [],
            gaAreas: [new GeneralAdmissionAreaDraft("GA East", 300)]);

        var manifest = Manifest.CreateFromTemplate(EventId.Create(), template);

        var area = Assert.Single(manifest.GeneralAdmissionAreas);
        Assert.Equal("GA East", area.Name.Value);
        Assert.Equal(300, area.Capacity.Value);
    }

    [Fact]
    public void CreateFromTemplate_SetsEventIdFromArgument()
    {
        var eventId = EventId.Create();
        var template = BuildTemplate(
            sections: [new SectionDraft("Main", [new RowDraft("A", [new SeatDraft("1")])])],
            gaAreas: []);

        var manifest = Manifest.CreateFromTemplate(eventId, template);

        Assert.Equal(eventId, manifest.EventId);
    }

    [Fact]
    public void CreateFromTemplate_CopiesVenueIdFromTemplate()
    {
        var venueId = VenueId.Create();
        var template = BuildTemplate(
            venueId: venueId,
            sections: [new SectionDraft("Main", [new RowDraft("A", [new SeatDraft("1")])])],
            gaAreas: []);

        var manifest = Manifest.CreateFromTemplate(EventId.Create(), template);

        Assert.Equal(venueId, manifest.VenueId);
    }

    [Fact]
    public void CreateFromTemplate_CopiesNameFromTemplate()
    {
        var template = BuildTemplate(
            name: "Arena Layout",
            sections: [new SectionDraft("Main", [new RowDraft("A", [new SeatDraft("1")])])],
            gaAreas: []);

        var manifest = Manifest.CreateFromTemplate(EventId.Create(), template);

        Assert.Equal("Arena Layout", manifest.Name.Value);
    }

    [Fact]
    public void CreateFromTemplate_IsNotFrozenByDefault()
    {
        var template = BuildTemplate(
            sections: [new SectionDraft("Main", [new RowDraft("A", [new SeatDraft("1")])])],
            gaAreas: []);

        var manifest = Manifest.CreateFromTemplate(EventId.Create(), template);

        Assert.False(manifest.IsFrozen);
    }

    // ── Freeze ────────────────────────────────────────────────────────────────

    [Fact]
    public void Freeze_SetsIsFrozenToTrue()
    {
        var manifest = CreateManifest();

        manifest.Freeze();

        Assert.True(manifest.IsFrozen);
    }

    [Fact]
    public void Freeze_WhenCalledTwice_IsIdempotent()
    {
        var manifest = CreateManifest();

        manifest.Freeze();
        manifest.Freeze();

        Assert.True(manifest.IsFrozen);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ManifestTemplate BuildTemplate(
        IReadOnlyList<SectionDraft> sections,
        IReadOnlyList<GeneralAdmissionAreaDraft> gaAreas,
        string? name = null,
        VenueId? venueId = null)
    {
        return ManifestTemplate.Create(
            new ManifestTemplateName(name ?? "Concert Layout"),
            null,
            venueId ?? VenueId.Create(),
            sections,
            gaAreas);
    }

    private static Manifest CreateManifest()
    {
        var template = BuildTemplate(
            sections: [new SectionDraft("Main", [new RowDraft("A", [new SeatDraft("1")])])],
            gaAreas: []);

        return Manifest.CreateFromTemplate(EventId.Create(), template);
    }
}

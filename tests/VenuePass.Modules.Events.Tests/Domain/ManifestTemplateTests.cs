using VenuePass.BuildingBlocks.Domain;
using VenuePass.Modules.Events.Domain.ManifestTemplates;
using VenuePass.Modules.Events.Domain.Venues;
using Xunit;

namespace VenuePass.Modules.Events.Tests.Domain;

public sealed class ManifestTemplateTests
{
    [Fact]
    public void ManifestTemplateCreate_WhenDescriptionIsNull_CreatesTemplate()
    {
        var template = ManifestTemplate.Create(
            new ManifestTemplateName("Template"),
            null,
            VenueId.Create(),
            [
                new SectionDraft(
                    "Main",
                    [new RowDraft("A", [new SeatDraft("1")])])
            ],
            []);

        Assert.Null(template.Description);
    }

    [Fact]
    public void ManifestTemplateCreate_WhenValuesContainWhitespace_TrimsStoredValues()
    {
        var template = ManifestTemplate.Create(
            new ManifestTemplateName("  Main Template  "),
            new ManifestTemplateDescription("  Layout for standard concerts  "),
            VenueId.Create(),
            [
                new SectionDraft(
                    "  Main Floor  ",
                    [
                        new RowDraft("  A  ", [new SeatDraft("  1  ")])
                    ])
            ],
            [new GeneralAdmissionAreaDraft("  GA East  ", 200)]);

        Assert.Equal("Main Template", template.Name.Value);
        Assert.Equal("Layout for standard concerts", template.Description!.Value);
        Assert.Equal("Main Floor", template.Sections.Single().Name.Value);
        Assert.Equal("A", template.Sections.Single().Rows.Single().Label.Value);
        Assert.Equal("1", template.Sections.Single().Rows.Single().Seats.Single().Label.Value);
        Assert.Equal("GA East", template.GeneralAdmissionAreas.Single().Name.Value);
    }

    [Fact]
    public void ManifestTemplateCreate_WhenStructureIsValid_CreatesTemplate()
    {
        var template = ManifestTemplate.Create(
            new ManifestTemplateName("Main Template"),
            new ManifestTemplateDescription("Layout for standard concerts"),
            VenueId.Create(),
            [
                new SectionDraft(
                    "A",
                    [
                        new RowDraft("A", [new SeatDraft("1"), new SeatDraft("2")])
                    ])
            ],
            [new GeneralAdmissionAreaDraft("GA", 200)]);

        Assert.NotEqual(Guid.Empty, template.Id);
        Assert.Equal("Main Template", template.Name.Value);
        Assert.Equal("Layout for standard concerts", template.Description!.Value);
        Assert.Single(template.Sections);
        Assert.Single(template.GeneralAdmissionAreas);
    }

    [Fact]
    public void ManifestTemplateCreate_WhenNoLayoutElements_ThrowsDomainRuleViolationException()
    {
        void Act() => ManifestTemplate.Create(
            new ManifestTemplateName("Template"),
            null,
            VenueId.Create(),
            [],
            []);

        var exception = Assert.Throws<DomainRuleViolationException>(Act);

        Assert.Equal("Events.ManifestTemplate.MustContainLayoutElements", exception.Code);
    }

    [Fact]
    public void ManifestTemplateCreate_WhenSectionDraftContainsNull_ThrowsArgumentNullException()
    {
        void Act() => ManifestTemplate.Create(
            new ManifestTemplateName("Template"),
            null,
            VenueId.Create(),
            [null!],
            []);

        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void ManifestTemplateCreate_WhenDuplicateLayoutElementNames_ThrowsDomainRuleViolationException()
    {
        void Act() => ManifestTemplate.Create(
            new ManifestTemplateName("Template"),
            null,
            VenueId.Create(),
            [
                new SectionDraft(
                    "Main",
                    [new RowDraft("A", [new SeatDraft("1")])])
            ],
            [new GeneralAdmissionAreaDraft("main", 100)]);

        var exception = Assert.Throws<DomainRuleViolationException>(Act);

        Assert.Equal("Events.ManifestTemplate.LayoutElement.DuplicateName", exception.Code);
    }

    [Fact]
    public void ManifestTemplateCreate_WhenDuplicateLayoutElementNamesDifferByCaseAndWhitespace_ThrowsDomainRuleViolationException()
    {
        void Act() => ManifestTemplate.Create(
            new ManifestTemplateName("Template"),
            null,
            VenueId.Create(),
            [
                new SectionDraft(
                    "  Main  ",
                    [new RowDraft("A", [new SeatDraft("1")])])
            ],
            [new GeneralAdmissionAreaDraft("main", 100)]);

        var exception = Assert.Throws<DomainRuleViolationException>(Act);

        Assert.Equal("Events.ManifestTemplate.LayoutElement.DuplicateName", exception.Code);
    }

    [Fact]
    public void ManifestTemplateCreate_WhenSectionHasNoRows_ThrowsDomainRuleViolationException()
    {
        void Act() => ManifestTemplate.Create(
            new ManifestTemplateName("Template"),
            null,
            VenueId.Create(),
            [new SectionDraft("Main", [])],
            []);

        var exception = Assert.Throws<DomainRuleViolationException>(Act);

        Assert.Equal("Events.ManifestTemplate.Section.MustContainRows", exception.Code);
    }

    [Fact]
    public void ManifestTemplateCreate_WhenDuplicateRowLabelsInSection_ThrowsDomainRuleViolationException()
    {
        void Act() => ManifestTemplate.Create(
            new ManifestTemplateName("Template"),
            null,
            VenueId.Create(),
            [
                new SectionDraft(
                    "Main",
                    [
                        new RowDraft("A", [new SeatDraft("1")]),
                        new RowDraft("a", [new SeatDraft("2")])
                    ])
            ],
            []);

        var exception = Assert.Throws<DomainRuleViolationException>(Act);

        Assert.Equal("Events.ManifestTemplate.Row.DuplicateLabel", exception.Code);
    }

    [Fact]
    public void ManifestTemplateCreate_WhenRowDraftContainsNull_ThrowsArgumentNullException()
    {
        void Act() => ManifestTemplate.Create(
            new ManifestTemplateName("Template"),
            null,
            VenueId.Create(),
            [
                new SectionDraft(
                    "Main",
                    [null!])
            ],
            []);

        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void ManifestTemplateCreate_WhenDuplicateRowLabelsDifferByCaseAndWhitespace_ThrowsDomainRuleViolationException()
    {
        void Act() => ManifestTemplate.Create(
            new ManifestTemplateName("Template"),
            null,
            VenueId.Create(),
            [
                new SectionDraft(
                    "Main",
                    [
                        new RowDraft("  A  ", [new SeatDraft("1")]),
                        new RowDraft("a", [new SeatDraft("2")])
                    ])
            ],
            []);

        var exception = Assert.Throws<DomainRuleViolationException>(Act);

        Assert.Equal("Events.ManifestTemplate.Row.DuplicateLabel", exception.Code);
    }

    [Fact]
    public void ManifestTemplateCreate_WhenRowHasNoSeats_ThrowsDomainRuleViolationException()
    {
        void Act() => ManifestTemplate.Create(
            new ManifestTemplateName("Template"),
            null,
            VenueId.Create(),
            [
                new SectionDraft(
                    "Main",
                    [new RowDraft("A", [])])
            ],
            []);

        var exception = Assert.Throws<DomainRuleViolationException>(Act);

        Assert.Equal("Events.ManifestTemplate.Row.MustContainSeats", exception.Code);
    }

    [Fact]
    public void ManifestTemplateCreate_WhenDuplicateSeatLabelsInRow_ThrowsDomainRuleViolationException()
    {
        void Act() => ManifestTemplate.Create(
            new ManifestTemplateName("Template"),
            null,
            VenueId.Create(),
            [
                new SectionDraft(
                    "Main",
                    [
                        new RowDraft("A", [new SeatDraft("1"), new SeatDraft("1")])
                    ])
            ],
            []);

        var exception = Assert.Throws<DomainRuleViolationException>(Act);

        Assert.Equal("Events.ManifestTemplate.Seat.DuplicateLabel", exception.Code);
    }

    [Fact]
    public void ManifestTemplateCreate_WhenSeatDraftContainsNull_ThrowsArgumentNullException()
    {
        void Act() => ManifestTemplate.Create(
            new ManifestTemplateName("Template"),
            null,
            VenueId.Create(),
            [
                new SectionDraft(
                    "Main",
                    [new RowDraft("A", [null!])])
            ],
            []);

        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void ManifestTemplateCreate_WhenGeneralAdmissionAreaDraftContainsNull_ThrowsArgumentNullException()
    {
        void Act() => ManifestTemplate.Create(
            new ManifestTemplateName("Template"),
            null,
            VenueId.Create(),
            [
                new SectionDraft(
                    "Main",
                    [new RowDraft("A", [new SeatDraft("1")])])
            ],
            [null!]);

        Assert.Throws<ArgumentNullException>(Act);
    }

    [Fact]
    public void ManifestTemplateCreate_WhenDuplicateSeatLabelsDifferByCaseAndWhitespace_ThrowsDomainRuleViolationException()
    {
        void Act() => ManifestTemplate.Create(
            new ManifestTemplateName("Template"),
            null,
            VenueId.Create(),
            [
                new SectionDraft(
                    "Main",
                    [
                        new RowDraft("A", [new SeatDraft("  A1  "), new SeatDraft("a1")])
                    ])
            ],
            []);

        var exception = Assert.Throws<DomainRuleViolationException>(Act);

        Assert.Equal("Events.ManifestTemplate.Seat.DuplicateLabel", exception.Code);
    }


    [Fact]
    public void GeneralAdmissionCapacity_WhenNotPositive_ThrowsArgumentOutOfRangeException()
    {
        void Act() => _ = new GeneralAdmissionCapacity(0);

        Assert.Throws<ArgumentOutOfRangeException>(Act);
    }
}
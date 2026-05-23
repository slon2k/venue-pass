using VenuePass.BuildingBlocks.Domain;

namespace VenuePass.Modules.Events.Domain.ManifestTemplates;

public static class ManifestTemplateErrors
{
    public static DomainError MustContainLayoutElements() => new(
        "Events.ManifestTemplate.MustContainLayoutElements",
        "Manifest template must contain at least one layout element.");

    public static DomainError SectionMustContainRows(string sectionName) => new(
        "Events.ManifestTemplate.Section.MustContainRows",
        $"Section '{sectionName}' must contain at least one row.");

    public static DomainError RowMustContainSeats(string rowLabel) => new(
        "Events.ManifestTemplate.Row.MustContainSeats",
        $"Row '{rowLabel}' must contain at least one seat.");

    public static DomainError DuplicateLayoutElementName(string name) => new(
        "Events.ManifestTemplate.LayoutElement.DuplicateName",
        $"A layout element with name '{name}' already exists.");

    public static DomainError DuplicateRowLabel(string rowLabel, string sectionName) => new(
        "Events.ManifestTemplate.Row.DuplicateLabel",
        $"Row with label '{rowLabel}' already exists in section '{sectionName}'.");

    public static DomainError DuplicateSeatLabel(string seatLabel, string rowLabel) => new(
        "Events.ManifestTemplate.Seat.DuplicateLabel",
        $"Seat with label '{seatLabel}' already exists in row '{rowLabel}'.");
}
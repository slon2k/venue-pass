using VenuePass.BuildingBlocks.Domain;

namespace VenuePass.Modules.Events.Domain.Manifests;

public static class ManifestErrors
{
    public static DomainError MustContainLayoutElements() => new(
        "Events.Manifest.MustContainLayoutElements",
        "Manifest must contain at least one layout element.");

    public static DomainError SectionMustContainRows(string sectionName) => new(
        "Events.Manifest.Section.MustContainRows",
        $"Section '{sectionName}' must contain at least one row.");

    public static DomainError RowMustContainSeats(string rowLabel) => new(
        "Events.Manifest.Row.MustContainSeats",
        $"Row '{rowLabel}' must contain at least one seat.");

    public static DomainError DuplicateLayoutElementName(string name) => new(
        "Events.Manifest.LayoutElement.DuplicateName",
        $"A layout element with name '{name}' already exists.");

    public static DomainError DuplicateRowLabel(string rowLabel, string sectionName) => new(
        "Events.Manifest.Row.DuplicateLabel",
        $"Row with label '{rowLabel}' already exists in section '{sectionName}'.");

    public static DomainError DuplicateSeatLabel(string seatLabel, string rowLabel) => new(
        "Events.Manifest.Seat.DuplicateLabel",
        $"Seat with label '{seatLabel}' already exists in row '{rowLabel}'.");
}
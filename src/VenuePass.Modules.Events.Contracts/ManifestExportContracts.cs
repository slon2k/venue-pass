namespace VenuePass.Modules.Events.Contracts;

public sealed record ManifestExportDto(
    Guid ManifestId,
    Guid EventId,
    IReadOnlyList<SectionExportDto> Sections,
    IReadOnlyList<GeneralAdmissionAreaExportDto> GeneralAdmissionAreas);

public sealed record SectionExportDto(
    Guid SectionId,
    string Name,
    IReadOnlyList<RowExportDto> Rows);

public sealed record RowExportDto(
    Guid RowId,
    string Label,
    IReadOnlyList<SeatExportDto> Seats);

public sealed record SeatExportDto(
    Guid SeatId,
    string Label);

public sealed record GeneralAdmissionAreaExportDto(
    Guid AreaId,
    string Name,
    int Capacity);
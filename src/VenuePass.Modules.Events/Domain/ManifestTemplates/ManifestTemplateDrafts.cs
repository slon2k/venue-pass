    namespace VenuePass.Modules.Events.Domain.ManifestTemplates;
    
    public sealed record SectionDraft(
        string Name,
        IReadOnlyList<RowDraft> Rows);

    public sealed record RowDraft(
        string Label,
        IReadOnlyList<SeatDraft> Seats);

    public sealed record SeatDraft(
        string Label);

    public sealed record GeneralAdmissionAreaDraft(
        string Name,
        int Capacity);
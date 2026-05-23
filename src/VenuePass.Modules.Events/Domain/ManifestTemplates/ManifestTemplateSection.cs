using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;

namespace VenuePass.Modules.Events.Domain.ManifestTemplates;

public sealed class ManifestTemplateSection : Entity<ManifestTemplateSectionId>
{
    private readonly List<SeatRow> _rows = [];

    private ManifestTemplateSection(
        ManifestTemplateSectionId id,
        SectionName name)
        : base(id)
    {
        Name = name;
    }

    public SectionName Name { get; private set; }

    public IReadOnlyCollection<SeatRow> Rows => _rows.AsReadOnly();

    internal static ManifestTemplateSection Create(
        ManifestTemplateSectionId id,
        SectionDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var section = new ManifestTemplateSection(
            id,
            new SectionName(draft.Name));

        foreach (var rowDraft in draft.Rows)
        {
            section.AddRow(rowDraft);
        }

        return section;
    }

    private void AddRow(RowDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        if (_rows.Any(x => x.Label == draft.Label))
        {
            throw new ArgumentException(
                $"Row with label '{draft.Label}' already exists in section '{Name}'.");
        }

        _rows.Add(
            SeatRow.Create(
                SeatRowId.New(),
                draft));
    }
}

public sealed record ManifestTemplateSectionId(Guid Value)
{
    public static ManifestTemplateSectionId New() => new(Guid.NewGuid());
    public static implicit operator Guid(ManifestTemplateSectionId id) => id.Value;
    public static implicit operator ManifestTemplateSectionId(Guid value) => new(value);
};

public sealed record SectionName
{
    public const int MaxLength = 100;
    public string Value { get; }

    public SectionName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public static implicit operator string(SectionName name) => name.Value;

    public override string ToString() => Value;
}

public sealed record ManifestTemplateSectionLabel
{
    public const int MaxLength = 1000;
    public string Value { get; }

    public ManifestTemplateSectionLabel(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public static implicit operator string(ManifestTemplateSectionLabel label) => label.Value;

    public override string ToString() => Value;
}
using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;

namespace VenuePass.Modules.Events.Domain.ManifestTemplates;

public sealed class ManifestTemplateSection : Entity<ManifestTemplateSectionId>
{
    private readonly List<SeatRow> _rows = [];

    private ManifestTemplateSection()
    {
    }

    private ManifestTemplateSection(
        ManifestTemplateSectionId id,
        SectionName name)
        : base(id)
    {
        Name = name;
    }

    public SectionName Name { get; private set; } = null!;

    public IReadOnlyList<SeatRow> Rows => _rows.AsReadOnly();

    internal static ManifestTemplateSection Create(
        SectionName name,
        IReadOnlyList<RowDraft> rowDrafts)
    {
        ArgumentNullException.ThrowIfNull(rowDrafts);

        var section = new ManifestTemplateSection(
            ManifestTemplateSectionId.Create(),
            name);

        foreach (var rowDraft in rowDrafts)
        {
            section.AddRow(rowDraft);
        }

        if (section.Rows.Count == 0)
        {
            throw new ArgumentException(
                $"Section '{name}' must contain at least one row.");
        }

        return section;
    }

    private void AddRow(RowDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var label = new RowLabel(draft.Label);

        if (_rows.Any(x => HasSameLabel(x.Label.Value, label.Value)))
        {
            throw new ArgumentException(
                $"Row with label '{label}' already exists in section '{Name}'.");
        }

        _rows.Add(SeatRow.Create(label, draft.Seats));
    }

    private static bool HasSameLabel(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}

public readonly record struct ManifestTemplateSectionId(Guid Value)
{
    public static ManifestTemplateSectionId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(ManifestTemplateSectionId id) => id.Value;
    public override string ToString() => Value.ToString();
};

public sealed record SectionName
{
    public const int MaxLength = 100;
    public string Value { get; private set; }

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

using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;

namespace VenuePass.Modules.Events.Domain.ManifestTemplates;

public sealed class Section : Entity<SectionId>
{
    private readonly List<SeatRow> _rows = [];

    private Section()
    {
    }

    private Section(
        SectionId id,
        SectionName name)
        : base(id)
    {
        Name = name;
    }

    public SectionName Name { get; private set; } = null!;

    public IReadOnlyList<SeatRow> Rows => _rows.AsReadOnly();

    internal static Section Create(
        SectionName name,
        IReadOnlyList<RowDraft> rowDrafts)
    {
        ArgumentNullException.ThrowIfNull(rowDrafts);

        var section = new Section(
            SectionId.Create(),
            name);

        foreach (var rowDraft in rowDrafts)
        {
            section.AddRow(rowDraft);
        }

        if (section.Rows.Count == 0)
        {
            throw new DomainRuleViolationException(ManifestTemplateErrors.SectionMustContainRows(name.Value));
        }

        return section;
    }

    private void AddRow(RowDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var label = new RowLabel(draft.Label);

        if (_rows.Any(x => HasSameLabel(x.Label.Value, label.Value)))
        {
            throw new DomainRuleViolationException(ManifestTemplateErrors.DuplicateRowLabel(label.Value, Name.Value));
        }

        _rows.Add(SeatRow.Create(label, draft.Seats));
    }

    private static bool HasSameLabel(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}

public readonly record struct SectionId(Guid Value)
{
    public static SectionId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(SectionId id) => id.Value;
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

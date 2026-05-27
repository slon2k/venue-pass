using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;

using TemplateModel = VenuePass.Modules.Events.Domain.ManifestTemplates;

namespace VenuePass.Modules.Events.Domain.Manifests;

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

    internal static Section CreateFrom(TemplateModel.Section templateSection)
    {
        ArgumentNullException.ThrowIfNull(templateSection);

        var section = new Section(
            SectionId.Create(),
            new SectionName(templateSection.Name));

        foreach (var templateRow in templateSection.Rows)
        {
            section._rows.Add(SeatRow.CreateFrom(templateRow));
        }

        if (section.Rows.Count == 0)
        {
            throw new DomainRuleViolationException(ManifestErrors.SectionMustContainRows(section.Name.Value));
        }

        return section;
    }
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

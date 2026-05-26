using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;
using VenuePass.Modules.Events.Domain.Venues;

using TemplateModel = VenuePass.Modules.Events.Domain.ManifestTemplates;

namespace VenuePass.Modules.Events.Domain.Manifests;

public sealed class Manifest : AggregateRoot<ManifestId>
{
    private readonly List<Section> _sections = [];
    private readonly List<GeneralAdmissionArea> _generalAdmissionAreas = [];

    private Manifest()
    {
    }

    private Manifest(
        ManifestId id,
        ManifestName name,
        VenueId venueId)
        : base(id)
    {
        Name = name;
        VenueId = venueId;
    }

    public ManifestName Name { get; private set; } = null!;
    public VenueId VenueId { get; private set; }

    public IReadOnlyList<Section> Sections => _sections.AsReadOnly();
    public IReadOnlyList<GeneralAdmissionArea> GeneralAdmissionAreas => _generalAdmissionAreas.AsReadOnly();

    public static Manifest CreateFromTemplate(TemplateModel.ManifestTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        var manifest = new Manifest(
            ManifestId.Create(),
            new ManifestName(template.Name.Value),
            template.VenueId);

        foreach (var templateSection in template.Sections)
        {
            manifest._sections.Add(Section.CreateFrom(templateSection));
        }

        foreach (var templateArea in template.GeneralAdmissionAreas)
        {
            manifest._generalAdmissionAreas.Add(GeneralAdmissionArea.CreateFrom(templateArea));
        }

        if (manifest._sections.Count == 0 && manifest._generalAdmissionAreas.Count == 0)
        {
            throw new DomainRuleViolationException(ManifestErrors.MustContainLayoutElements());
        }

        return manifest;
    }
}

public readonly record struct ManifestId(Guid Value)
{
    public static ManifestId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(ManifestId id) => id.Value;
    public override string ToString() => Value.ToString();
}

public sealed record ManifestName
{
    public const int MaxLength = 100;
    public string Value { get; private set; }

    public ManifestName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public static implicit operator string(ManifestName name) => name.Value;

    public override string ToString() => Value;
}

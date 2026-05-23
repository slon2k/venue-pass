using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;
using VenuePass.Modules.Events.Domain.Venues;

namespace VenuePass.Modules.Events.Domain.ManifestTemplates;

public sealed class ManifestTemplate : AggregateRoot<ManifestTemplateId>
{
    private readonly List<ManifestTemplateSection> _sections = [];
    private readonly List<GeneralAdmissionArea> _generalAdmissionAreas = [];

    private ManifestTemplate()
    {
    }

    private ManifestTemplate(
        ManifestTemplateId id,
        ManifestTemplateName name,
        ManifestTemplateDescription? description,
        VenueId venueId)
        : base(id)
    {
        Name = name;
        Description = description;
        VenueId = venueId;
    }

    public ManifestTemplateName Name { get; private set; } = null!;
    public ManifestTemplateDescription? Description { get; private set; }
    public VenueId VenueId { get; private set; }

    public IReadOnlyList<ManifestTemplateSection> Sections => _sections.AsReadOnly();
    public IReadOnlyList<GeneralAdmissionArea> GeneralAdmissionAreas => _generalAdmissionAreas.AsReadOnly();

    public static ManifestTemplate Create(
        ManifestTemplateName name,
        ManifestTemplateDescription? description,
        VenueId venueId,
        IReadOnlyList<SectionDraft> sectionDrafts,
        IReadOnlyList<GeneralAdmissionAreaDraft> generalAdmissionAreaDrafts)
    {
        ArgumentNullException.ThrowIfNull(sectionDrafts);
        ArgumentNullException.ThrowIfNull(generalAdmissionAreaDrafts);
        ArgumentNullException.ThrowIfNull(name);

        var template = new ManifestTemplate(
            ManifestTemplateId.Create(),
            name,
            description,
            venueId);

        foreach (var sectionDraft in sectionDrafts)
        {
            template.AddSection(sectionDraft);
        }

        foreach (var areaDraft in generalAdmissionAreaDrafts)
        {
            template.AddGeneralAdmissionArea(areaDraft);
        }

        if (template._sections.Count == 0 && template._generalAdmissionAreas.Count == 0)
        {
            throw new ArgumentException("Manifest template must contain at least one layout element.");
        }

        return template;
    }

    private void AddSection(SectionDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var name = new SectionName(draft.Name);
  
        EnsureLayoutElementNameIsUnique(name.Value);

        _sections.Add(ManifestTemplateSection.Create(name, draft.Rows));
    }

    private void AddGeneralAdmissionArea(GeneralAdmissionAreaDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var name = new GeneralAdmissionAreaName(draft.Name);
        var capacity = new GeneralAdmissionCapacity(draft.Capacity);

        EnsureLayoutElementNameIsUnique(name.Value);

        _generalAdmissionAreas.Add(GeneralAdmissionArea.Create(name, capacity));
    }

    private void EnsureLayoutElementNameIsUnique(string candidate)
    {
        if (_sections.Any(x => HasSameName(x.Name.Value, candidate)) ||
            _generalAdmissionAreas.Any(x => HasSameName(x.Name.Value, candidate)))
        {
            throw new ArgumentException($"A layout element with name '{candidate}' already exists.");
        }
    }

    private static bool HasSameName(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}

public readonly record struct ManifestTemplateId(Guid Value)
{
    public static ManifestTemplateId Create() => new(Guid.CreateVersion7());
    public static implicit operator Guid(ManifestTemplateId id) => id.Value;
    public override string ToString() => Value.ToString();
}

public sealed record ManifestTemplateName
{
    public const int MaxLength = 100;
    public string Value { get; private set; }

    public ManifestTemplateName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public static implicit operator string(ManifestTemplateName name) => name.Value;

    public override string ToString() => Value;
}

public sealed record ManifestTemplateDescription
{
    public const int MaxLength = 1000;
    public string Value { get; private set; }

    public ManifestTemplateDescription(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        value = value.Trim();
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public static implicit operator string(ManifestTemplateDescription description) => description.Value;

    public override string ToString() => Value;
}
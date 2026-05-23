using VenuePass.BuildingBlocks.Domain;
using VenuePass.BuildingBlocks.Extensions;
using VenuePass.Modules.Events.Domain.Venues;

namespace VenuePass.Modules.Events.Domain.ManifestTemplates;

public sealed class ManifestTemplate : AggregateRoot<ManifestTemplateId>
{
    private readonly List<ManifestTemplateSection> _sections = [];
    private readonly List<GeneralAdmissionArea> _generalAdmissionAreas = [];

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

    public ManifestTemplateName Name { get; private set; }
    public ManifestTemplateDescription? Description { get; private set; }
    public VenueId VenueId { get; private set; }

    public IReadOnlyCollection<ManifestTemplateSection> Sections => _sections.AsReadOnly();
    public IReadOnlyCollection<GeneralAdmissionArea> GeneralAdmissionAreas => _generalAdmissionAreas.AsReadOnly();

    public static ManifestTemplate Create(
        ManifestTemplateId id,
        ManifestTemplateName name,
        ManifestTemplateDescription? description,
        VenueId venueId,
        IReadOnlyCollection<SectionDraft> sectionDrafts,
        IReadOnlyCollection<GeneralAdmissionAreaDraft> generalAdmissionAreaDrafts)
    {
        ArgumentNullException.ThrowIfNull(sectionDrafts);
        ArgumentNullException.ThrowIfNull(generalAdmissionAreaDrafts);

        var template = new ManifestTemplate(id, name, description, venueId);

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

        if (_sections.Any(x => x.Name == draft.Name))
        {
            throw new ArgumentException($"Section with name '{draft.Name}' already exists.");
        }

        _sections.Add(
            ManifestTemplateSection.Create(
                ManifestTemplateSectionId.New(),
                draft));
    }

    private void AddGeneralAdmissionArea(GeneralAdmissionAreaDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        if (_generalAdmissionAreas.Any(x => x.Name == draft.Name))
        {
            throw new ArgumentException($"General admission area with name '{draft.Name}' already exists.");
        }

        _generalAdmissionAreas.Add(
            GeneralAdmissionArea.Create(
                GeneralAdmissionAreaId.New(),
                draft.Name,
                draft.Capacity));
    }
}

public readonly record struct ManifestTemplateId(Guid Value)
{
    public static ManifestTemplateId New() => new(Guid.NewGuid());
    public static implicit operator Guid(ManifestTemplateId id) => id.Value;
    public static implicit operator ManifestTemplateId(Guid value) => new(value);
}

public sealed record ManifestTemplateName
{
    public const int MaxLength = 100;
    public string Value { get; }

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
    public string Value { get; }

    public ManifestTemplateDescription(string value)
    {
        value = value?.Trim() ?? string.Empty;
        value.ThrowIfTooLong(nameof(value), MaxLength);
        Value = value;
    }

    public static implicit operator string(ManifestTemplateDescription description) => description.Value;

    public override string ToString() => Value;
}
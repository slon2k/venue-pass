using VenuePass.Modules.Events.Domain.ManifestTemplates;
using Xunit;

namespace VenuePass.Modules.Events.Tests.Domain;

public sealed class ManifestTemplateValueObjectsTests
{
    [Fact]
    public void ManifestTemplateName_WhenInputsNormalize_AreEqual()
    {
        var left = new ManifestTemplateName("  Main Template  ");
        var right = new ManifestTemplateName("Main Template");

        Assert.Equal(left, right);
    }

    [Fact]
    public void SectionName_WhenInputsNormalize_AreEqual()
    {
        var left = new SectionName("  A  ");
        var right = new SectionName("A");

        Assert.Equal(left, right);
    }

    [Fact]
    public void SeatLabel_WhenInputsNormalize_AreEqual()
    {
        var left = new SeatLabel("  A1  ");
        var right = new SeatLabel("A1");

        Assert.Equal(left, right);
    }

    [Fact]
    public void GeneralAdmissionAreaName_WhenInputsNormalize_AreEqual()
    {
        var left = new GeneralAdmissionAreaName("  GA East  ");
        var right = new GeneralAdmissionAreaName("GA East");

        Assert.Equal(left, right);
    }

    [Fact]
    public void ManifestTemplateDescription_WhenNullOrWhitespace_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _ = new ManifestTemplateDescription(null!));
        Assert.Throws<ArgumentException>(() => _ = new ManifestTemplateDescription("   "));
    }

    [Fact]
    public void StringValueObjects_WhenAtMaxLength_DoNotThrow()
    {
        _ = new ManifestTemplateName(new string('a', ManifestTemplateName.MaxLength));
        _ = new ManifestTemplateDescription(new string('a', ManifestTemplateDescription.MaxLength));
        _ = new SectionName(new string('a', SectionName.MaxLength));
        _ = new RowLabel(new string('a', RowLabel.MaxLength));
        _ = new SeatLabel(new string('a', SeatLabel.MaxLength));
        _ = new GeneralAdmissionAreaName(new string('a', GeneralAdmissionAreaName.MaxLength));
    }

    [Fact]
    public void StringValueObjects_WhenExceedingMaxLength_ThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _ = new ManifestTemplateName(new string('a', ManifestTemplateName.MaxLength + 1)));
        Assert.Throws<ArgumentException>(() => _ = new ManifestTemplateDescription(new string('a', ManifestTemplateDescription.MaxLength + 1)));
        Assert.Throws<ArgumentException>(() => _ = new SectionName(new string('a', SectionName.MaxLength + 1)));
        Assert.Throws<ArgumentException>(() => _ = new RowLabel(new string('a', RowLabel.MaxLength + 1)));
        Assert.Throws<ArgumentException>(() => _ = new SeatLabel(new string('a', SeatLabel.MaxLength + 1)));
        Assert.Throws<ArgumentException>(() => _ = new GeneralAdmissionAreaName(new string('a', GeneralAdmissionAreaName.MaxLength + 1)));
    }
}
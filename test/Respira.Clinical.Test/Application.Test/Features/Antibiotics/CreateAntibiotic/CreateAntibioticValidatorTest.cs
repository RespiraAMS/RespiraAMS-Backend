using Application.Features.Antibiotics.CreateAntibiotic;
using Domain.Enums;

namespace Application.Test.Features.Antibiotics.CreateAntibiotic;

public class CreateAntibioticValidatorTest
{
    private readonly CreateAntibioticValidator _validator = new();

    # region Valid command

    [Theory]
    [InlineData("Amoxicillin", "Access", "Oral", "500 mg orally every 8 hours")]
    [InlineData("Meropenem", "Watch", "Intravenous", "1 g IV every 8 hours")]
    public async Task CreateAntibiotic_Success(string name, string classification, string route,
        string standardDose)
    {
        var result = await _validator.ValidateAsync(new CreateAntibioticCommand
        {
            Name = name,
            AntibioticGroupId = Guid.CreateVersion7(),
            Classification = Enum.Parse<AwareClassification>(classification),
            RouteOfAdministration = Enum.Parse<RouteOfAdministration>(route),
            StandardDose = standardDose,
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    private const string ValidGroupId = "0198A6C4-5D3E-7F2A-9B1C-3E4F5A6B7C8D";
    private const string EmptyGroupId = "00000000-0000-0000-0000-000000000000";

    // Classification/route are enum raw values: 0 is a defined member (in range),
    // 999 is outside every defined member (IsInEnum boundary)
    [Theory]
    [InlineData("", ValidGroupId, 0, 0, "some dose", "Name")]
    [InlineData("   ", ValidGroupId, 0, 0, "some dose", "Name")]
    [InlineData("some name", EmptyGroupId, 0, 0, "some dose", "AntibioticGroupId")]
    [InlineData("some name", ValidGroupId, 999, 0, "some dose", "Classification")]
    [InlineData("some name", ValidGroupId, 0, 999, "some dose", "RouteOfAdministration")]
    [InlineData("some name", ValidGroupId, 0, 0, "", "StandardDose")]
    [InlineData("some name", ValidGroupId, 0, 0, " ", "StandardDose")]
    public async Task CreateAntibiotic_Fail(string name, string groupId, int classification,
        int route, string standardDose, string property)
    {
        var result = await _validator.ValidateAsync(new CreateAntibioticCommand
        {
            Name = name,
            AntibioticGroupId = Guid.Parse(groupId),
            Classification = (AwareClassification)classification,
            RouteOfAdministration = (RouteOfAdministration)route,
            StandardDose = standardDose,
        }, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal(property, result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task CreateAntibiotic_AllFieldsInvalid_Fail()
    {
        var result = await _validator.ValidateAsync(new CreateAntibioticCommand
        {
            Name = "",
            AntibioticGroupId = Guid.Empty,
            Classification = (AwareClassification)999,
            RouteOfAdministration = (RouteOfAdministration)999,
            StandardDose = "",
        }, TestContext.Current.CancellationToken);

        Assert.Equal(5, result.Errors.Count);
        Assert.Contains(result.Errors, x => x.PropertyName == "Name");
        Assert.Contains(result.Errors, x => x.PropertyName == "AntibioticGroupId");
        Assert.Contains(result.Errors, x => x.PropertyName == "Classification");
        Assert.Contains(result.Errors, x => x.PropertyName == "RouteOfAdministration");
        Assert.Contains(result.Errors, x => x.PropertyName == "StandardDose");
    }

    # endregion
}

using Application.Features.Antibiotics.UpdateAntibiotic;
using Domain.Enums;

namespace Application.Test.Features.Antibiotics.UpdateAntibiotic;

public class UpdateAntibioticValidatorTest
{
    private readonly UpdateAntibioticValidator _validator = new();

    # region Valid command

    public static readonly TheoryData<Guid, string, AwareClassification> ValidCommands =
    [
        (Guid.CreateVersion7(), "Amoxicillin", AwareClassification.Access),
        (Guid.CreateVersion7(), "Meropenem", AwareClassification.Watch),
    ];

    [Theory]
    [MemberData(nameof(ValidCommands))]
    public async Task UpdateAntibiotic_Success(Guid id, string name, AwareClassification classification)
    {
        var result = await _validator.ValidateAsync(new UpdateAntibioticCommand
        {
            Id = id,
            Name = name,
            AntibioticGroupId = Guid.CreateVersion7(),
            Classification = classification,
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    private const string ValidGroupId = "0198A6C4-5D3E-7F2A-9B1C-3E4F5A6B7C8D";
    private const string EmptyGroupId = "00000000-0000-0000-0000-000000000000";

    // Classification raw values: 0 is a defined member (in range), 999 is outside
    // every defined member (IsInEnum boundary)
    [Theory]
    [InlineData(EmptyGroupId, "some name", ValidGroupId, 0, "Id")]
    [InlineData(ValidGroupId, "", ValidGroupId, 0, "Name")]
    [InlineData(ValidGroupId, "   ", ValidGroupId, 0, "Name")]
    [InlineData(ValidGroupId, "some name", EmptyGroupId, 0, "AntibioticGroupId")]
    [InlineData(ValidGroupId, "some name", ValidGroupId, 999, "Classification")]
    public async Task UpdateAntibiotic_Fail(string id, string name, string groupId, int classification,
        string property)
    {
        var result = await _validator.ValidateAsync(new UpdateAntibioticCommand
        {
            Id = Guid.Parse(id),
            Name = name,
            AntibioticGroupId = Guid.Parse(groupId),
            Classification = (AwareClassification)classification,
        }, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal(property, result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task UpdateAntibiotic_AllFieldsInvalid_Fail()
    {
        var result = await _validator.ValidateAsync(new UpdateAntibioticCommand
        {
            Id = Guid.Empty,
            Name = "",
            AntibioticGroupId = Guid.Empty,
            Classification = (AwareClassification)999,
        }, TestContext.Current.CancellationToken);

        Assert.Equal(4, result.Errors.Count);
        Assert.Contains(result.Errors, x => x.PropertyName == "Id");
        Assert.Contains(result.Errors, x => x.PropertyName == "Name");
        Assert.Contains(result.Errors, x => x.PropertyName == "AntibioticGroupId");
        Assert.Contains(result.Errors, x => x.PropertyName == "Classification");
    }

    # endregion
}

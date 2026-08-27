using Application.Features.AntibioticGroups.UpdateAntibioticGroup;

namespace Application.Test.Features.AntibioticGroups.UpdateAntibioticGroup;

public class UpdateAntibioticGroupValidatorTest
{
    private readonly UpdateAntibioticGroupValidator _validator = new();

    # region Valid command

    public static readonly TheoryData<Guid, string, string> ValidCommands =
    [
        (Guid.CreateVersion7(), "Beta-lactams", "Cell wall synthesis inhibitors sharing the beta-lactam ring"),
        (Guid.CreateVersion7(), "Macrolides", "Protein synthesis inhibitors with a macrocyclic lactone ring"),
    ];

    [Theory]
    [MemberData(nameof(ValidCommands))]
    public async Task UpdateAntibioticGroup_RootGroup_Success(Guid id, string name, string description)
    {
        var result = await _validator.ValidateAsync(new UpdateAntibioticGroupCommand
        {
            Id = id,
            Name = name,
            Description = description,
            ParentId = null,
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task UpdateAntibioticGroup_WithParentId_Success()
    {
        var result = await _validator.ValidateAsync(new UpdateAntibioticGroupCommand
        {
            Id = Guid.CreateVersion7(),
            Name = "Penicillins",
            Description = "Subgroup of beta-lactam antibiotics",
            ParentId = Guid.CreateVersion7(),
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    [Theory]
    [InlineData("0198A6C4-5D3E-7F2A-9B1C-3E4F5A6B7C8D", "", "some description", "Name")]
    [InlineData("0198A6C4-5D3E-7F2A-9B1C-3E4F5A6B7C8D", "   ", "some description", "Name")]
    [InlineData("0198A6C4-5D3E-7F2A-9B1C-3E4F5A6B7C8D", "some name", "", "Description")]
    [InlineData("0198A6C4-5D3E-7F2A-9B1C-3E4F5A6B7C8D", "some name", " ", "Description")]
    // Boundary: Guid.Empty is treated as empty by NotEmpty
    [InlineData("00000000-0000-0000-0000-000000000000", "some name", "some description", "Id")]
    public async Task UpdateAntibioticGroup_Fail(string id, string name, string description, string property)
    {
        var result = await _validator.ValidateAsync(new UpdateAntibioticGroupCommand
        {
            Id = Guid.Parse(id),
            Name = name,
            Description = description,
            ParentId = null,
        }, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal(property, result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task UpdateAntibioticGroup_EmptyParentIdPassesValidation_RejectedByHandlerInstead()
    {
        var result = await _validator.ValidateAsync(new UpdateAntibioticGroupCommand
        {
            Id = Guid.CreateVersion7(),
            Name = "some name",
            Description = "some description",
            ParentId = Guid.Empty,
        }, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("ParentId", result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task UpdateAntibioticGroup_AllFieldsEmpty_Fail()
    {
        var result = await _validator.ValidateAsync(new UpdateAntibioticGroupCommand
        {
            Id = Guid.Empty,
            Name = "",
            Description = "",
            ParentId = null,
        }, TestContext.Current.CancellationToken);

        Assert.Equal(3, result.Errors.Count);
        Assert.Contains(result.Errors, x => x.PropertyName == "Id");
        Assert.Contains(result.Errors, x => x.PropertyName == "Name");
        Assert.Contains(result.Errors, x => x.PropertyName == "Description");
    }

    # endregion
}

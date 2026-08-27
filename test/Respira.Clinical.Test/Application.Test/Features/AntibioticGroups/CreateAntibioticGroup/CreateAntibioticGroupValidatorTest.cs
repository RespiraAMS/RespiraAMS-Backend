using Application.Features.AntibioticGroups.CreateAntibioticGroup;

namespace Application.Test.Features.AntibioticGroups.CreateAntibioticGroup;

public class CreateAntibioticGroupValidatorTest
{
    private readonly CreateAntibioticGroupValidator _validator = new();

    # region Valid command

    [Theory]
    [InlineData("Beta-lactams", "Cell wall synthesis inhibitors sharing the beta-lactam ring")]
    [InlineData("Macrolides", "Protein synthesis inhibitors with a macrocyclic lactone ring")]
    public async Task CreateAntibioticGroup_Success(string name, string description)
    {
        var result = await _validator.ValidateAsync(new CreateAntibioticGroupCommand
        {
            Name = name,
            Description = description,
            ParentId = null,
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task CreateAntibioticGroup_WithParentId_Success()
    {
        var result = await _validator.ValidateAsync(new CreateAntibioticGroupCommand
        {
            Name = "Penicillins",
            Description = "Subgroup of beta-lactam antibiotics",
            ParentId = Guid.CreateVersion7(),
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    [Theory]
    [InlineData("", "some description", "Name")]
    [InlineData("   ", "some description", "Name")]
    [InlineData("some name", "", "Description")]
    [InlineData("some name", " ", "Description")]
    public async Task CreateAntibioticGroup_Fail(string name, string description, string property)
    {
        var result = await _validator.ValidateAsync(new CreateAntibioticGroupCommand
        {
            Name = name,
            Description = description,
            ParentId = null,
        }, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal(property, result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task CreateAntibioticGroup_EmptyParentId_Fail()
    {
        var result = await _validator.ValidateAsync(new CreateAntibioticGroupCommand
        {
            Name = "Cephalosporins",
            Description = "Beta-lactam antibiotics resistant to staphylococcal beta-lactamase",
            ParentId = Guid.Empty,
        }, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("ParentId", result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task CreateAntibioticGroup_AllFieldsEmpty_Fail()
    {
        var result = await _validator.ValidateAsync(new CreateAntibioticGroupCommand
        {
            Name = "",
            Description = "",
            ParentId = Guid.Empty,
        }, TestContext.Current.CancellationToken);

        Assert.Equal(3, result.Errors.Count);
        Assert.Contains(result.Errors, x => x.PropertyName == "Name");
        Assert.Contains(result.Errors, x => x.PropertyName == "Description");
        Assert.Contains(result.Errors, x => x.PropertyName == "ParentId");
    }

    # endregion
}

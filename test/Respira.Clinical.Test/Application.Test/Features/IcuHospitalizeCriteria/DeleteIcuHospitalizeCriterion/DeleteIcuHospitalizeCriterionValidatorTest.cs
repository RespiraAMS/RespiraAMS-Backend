using Application.Features.IcuHospitalizeCriteria.DeleteIcuHospitalizeCriterion;

namespace Application.Test.Features.IcuHospitalizeCriteria.DeleteIcuHospitalizeCriterion;

public class DeleteIcuHospitalizeCriterionValidatorTest
{
    private readonly DeleteIcuHospitalizeCriterionValidator _validator = new();

    # region Valid command

    [Fact]
    public async Task DeleteIcuHospitalizeCriterion_ValidId_Success()
    {
        var result = await _validator.ValidateAsync(new DeleteIcuHospitalizeCriterionCommand(Guid.CreateVersion7()), TestContext.Current.CancellationToken);
        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    public static readonly TheoryData<Guid> InvalidIds =
    [
        // Boundary: empty GUID violates NotEmpty on Id
        Guid.Empty,
    ];

    [Theory]
    [MemberData(nameof(InvalidIds))]
    public async Task DeleteIcuHospitalizeCriterion_Fail(Guid id)
    {
        var result = await _validator.ValidateAsync(new DeleteIcuHospitalizeCriterionCommand(id), TestContext.Current.CancellationToken);
        Assert.Contains(result.Errors, x => x.PropertyName == "Id");
    }

    # endregion
}

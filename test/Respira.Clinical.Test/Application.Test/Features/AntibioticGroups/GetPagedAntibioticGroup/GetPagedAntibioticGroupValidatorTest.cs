using Application.Features.AntibioticGroups.GetPagedAntibioticGroup;
using Respira.ServiceDefaults.Dtos;

namespace Application.Test.Features.AntibioticGroups.GetPagedAntibioticGroup;

public class GetPagedAntibioticGroupValidatorTest
{
    private readonly GetPagedAntibioticGroupValidator _validator = new();

    # region Valid pagination param

    // Boundaries of IsValidPaginationParam: page >= 1, 1 <= size <= 100
    public static readonly TheoryData<int, int> ValidParams =
    [
        (1, 1),   // smallest allowed values
        (1, 10),  // default size
        (1, 100), // largest allowed size
        (7, 50),
    ];

    [Theory]
    [MemberData(nameof(ValidParams))]
    public async Task GetPagedAntibioticGroup_Success(int page, int size)
    {
        var result = await _validator.ValidateAsync(new GetPagedAntibioticGroupQuery
        {
            Param = new PaginationParam { Page = page, Size = size },
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid pagination param

    public static readonly TheoryData<int, int> InvalidParams =
    [
        (0, 10),   // page lower boundary violation
        (-3, 10),  // negative page
        (1, 0),    // size lower boundary violation
        (1, -20),  // negative size
        (1, 101),  // size upper boundary violation
    ];

    [Theory]
    [MemberData(nameof(InvalidParams))]
    public async Task GetPagedAntibioticGroup_Fail(int page, int size)
    {
        var result = await _validator.ValidateAsync(new GetPagedAntibioticGroupQuery
        {
            Param = new PaginationParam { Page = page, Size = size },
        }, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("Param", result.Errors[0].PropertyName);
    }

    # endregion
}

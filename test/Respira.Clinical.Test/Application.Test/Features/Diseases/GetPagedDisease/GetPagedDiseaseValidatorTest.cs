using Application.Features.Diseases.GetPagedDisease;
using Respira.ServiceDefaults.Dtos;

namespace Application.Test.Features.Diseases.GetPagedDisease;

public class GetPagedDiseaseValidatorTest
{
    private readonly GetPagedDiseaseValidator _validator = new();

    private static GetPagedDiseaseQuery Query(int page, int size) => new()
    {
        Param = new PaginationParam { Page = page, Size = size },
    };

    # region Valid command

    [Fact]
    public async Task GetPagedDisease_ValidCommand_Success()
    {
        var result = await _validator.ValidateAsync(Query(1, 10), TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    // Boundary value technique: Page = 1 is the smallest valid page index
    [Fact]
    public async Task GetPagedDisease_PageBoundaryMin_Success()
    {
        var result = await _validator.ValidateAsync(Query(1, 1), TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    // Boundary value technique: Size = 100 is the largest valid page size
    [Fact]
    public async Task GetPagedDisease_SizeBoundaryMax_Success()
    {
        var result = await _validator.ValidateAsync(Query(1, 100), TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    // Boundary value technique: Page = 0 is just below the valid range (must be > 0)
    [Fact]
    public async Task GetPagedDisease_PageZero_Fail()
    {
        var result = await _validator.ValidateAsync(Query(0, 10), TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("Param", result.Errors[0].PropertyName);
    }

    // Boundary value technique: Size = 0 is just below the valid range (must be > 0)
    [Fact]
    public async Task GetPagedDisease_SizeZero_Fail()
    {
        var result = await _validator.ValidateAsync(Query(1, 0), TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("Param", result.Errors[0].PropertyName);
    }

    // Boundary value technique: Size = 101 is just above the valid range (must be <= 100)
    [Fact]
    public async Task GetPagedDisease_SizeOverMax_Fail()
    {
        var result = await _validator.ValidateAsync(Query(1, 101), TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("Param", result.Errors[0].PropertyName);
    }

    # endregion
}

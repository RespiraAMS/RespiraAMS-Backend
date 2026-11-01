using Respira.Saga.Application.Features.Audit.Queries.Search;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Saga.Application.Test.Features.Audit.Search;

public sealed class SearchSagaAuditValidatorTest
{
    private readonly SearchSagaAuditValidator _validator = new();

    [Fact]
    public async Task ValidFiltersAndPagination_Pass()
    {
        var result = await _validator.ValidateAsync(new SearchSagaAuditQuery
        {
            From = DateTimeOffset.UtcNow.AddDays(-1),
            To = DateTimeOffset.UtcNow,
            Param = new PaginationParam { Page = 1, Size = 50 },
        }, TestContext.Current.CancellationToken);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task InvalidPagination_Fails(int page, int size)
    {
        var result = await _validator.ValidateAsync(new SearchSagaAuditQuery
        {
            Param = new PaginationParam { Page = page, Size = size },
        }, TestContext.Current.CancellationToken);

        Assert.Contains(result.Errors, error => error.PropertyName == "Param");
    }

    [Fact]
    public async Task FromAfterTo_Fails()
    {
        var result = await _validator.ValidateAsync(new SearchSagaAuditQuery
        {
            From = DateTimeOffset.UtcNow,
            To = DateTimeOffset.UtcNow.AddDays(-1),
        }, TestContext.Current.CancellationToken);

        Assert.Contains(result.Errors, error => error.PropertyName == "");
    }
}

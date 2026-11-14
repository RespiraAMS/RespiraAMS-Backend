using Respira.Authentication.Application.Features.Authentication.Queries.GetList;
using Respira.Authentication.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Authentication.Infrastructure.Test.Features.GetList;

public sealed class GetListAccountValidatorTest
{
    private readonly GetListAccountValidator _validator = new();

    public static TheoryData<int, int> ValidPagination() =>
        new() { { 1, 1 }, { 1, 10 }, { 1, 100 }, { 2, 50 }, { 5, 25 } };

    [Theory]
    [MemberData(nameof(ValidPagination))]
    public void Validate_WithValidPagination_ReturnsNoErrors(int page, int size)
    {
        var result = _validator.Validate(BuildQuery(page, size));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(RoleType.Doctor)]
    [InlineData(RoleType.HeadDepartmentDoctor)]
    [InlineData(RoleType.MedicalRegulationReviewer)]
    [InlineData(RoleType.Admin)]
    public void Validate_WithRoleFilterAndDefaultPagination_ReturnsNoErrors(RoleType role)
    {
        var result = _validator.Validate(BuildQuery(1, 10, role, StatusType.Active, true));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithInvalidRoleEnum_ReturnsErrorOnFilterRole()
    {
        var result = _validator.Validate(BuildQuery(1, 10, (RoleType)999));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Filter.Role", error.PropertyName);
    }

    [Fact]
    public void Validate_WithInvalidStatusEnum_ReturnsErrorOnFilterStatus()
    {
        var result = _validator.Validate(BuildQuery(1, 10, null, (StatusType)999));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Filter.Status", error.PropertyName);
    }

    [Fact]
    public void Validate_WithPageZero_ReturnsErrorOnPaginationParam()
    {
        var result = _validator.Validate(BuildQuery(0, 10));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("PaginationParam", error.PropertyName);
        Assert.Equal("Pagination page must be a positive integer", error.ErrorMessage);
    }

    [Fact]
    public void Validate_WithNegativePage_ReturnsErrorOnPaginationParam()
    {
        var result = _validator.Validate(BuildQuery(-2, 10));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("PaginationParam", error.PropertyName);
        Assert.Equal("Pagination page must be a positive integer", error.ErrorMessage);
    }

    [Fact]
    public void Validate_WithSizeZero_ReturnsErrorOnPaginationParam()
    {
        var result = _validator.Validate(BuildQuery(1, 0));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("PaginationParam", error.PropertyName);
        Assert.Equal(
            "Pagination size must be a positive integer and less than or equal to 100",
            error.ErrorMessage
        );
    }

    [Fact]
    public void Validate_WithSizeAboveMaximum_ReturnsErrorOnPaginationParam()
    {
        var result = _validator.Validate(BuildQuery(1, 101));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("PaginationParam", error.PropertyName);
        Assert.Equal(
            "Pagination size must be a positive integer and less than or equal to 100",
            error.ErrorMessage
        );
    }

    [Fact]
    public void Validate_WithInvalidPageAndSize_ReturnsTwoErrorsOnPaginationParam()
    {
        var result = _validator.Validate(BuildQuery(0, 500));

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.All(result.Errors, x => Assert.Equal("PaginationParam", x.PropertyName));
    }

    private static GetListAccountQuery BuildQuery(
        int page,
        int size,
        RoleType? role = null,
        StatusType? status = null,
        bool? isEmailConfirmed = null
    ) =>
        new()
        {
            PaginationParam = new PaginationParam { Page = page, Size = size },
            Filter = new AccountFilter
            {
                Role = role,
                Status = status,
                IsEmailConfirmed = isEmailConfirmed,
            },
        };
}

using Respira.Doctor.Application.Features.Doctors.Queries.GetPagedDoctor;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Doctor.Application.Test.Features.Doctors.Queries.GetPagedDoctor;

public sealed class GetPagedDoctorValidatorTest
{
    private readonly GetPagedDoctorValidator _validator = new();

    public static TheoryData<int, int> ValidPagination() =>
        new() { { 1, 1 }, { 1, 10 }, { 1, 100 }, { 2, 50 }, { 7, 25 } };

    [Theory]
    [MemberData(nameof(ValidPagination))]
    public void Validate_WithValidPagination_ReturnsNoErrors(int page, int size)
    {
        var result = _validator.Validate(BuildQuery(page, size));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithDefaultPagination_ReturnsNoErrors()
    {
        var result = _validator.Validate(BuildQuery(1, 10));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithPageZero_ReturnsErrorOnParam()
    {
        var result = _validator.Validate(BuildQuery(0, 10));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Param", error.PropertyName);
        Assert.Equal("Pagination page must be a positive integer", error.ErrorMessage);
    }

    [Fact]
    public void Validate_WithNegativePage_ReturnsErrorOnParam()
    {
        var result = _validator.Validate(BuildQuery(-1, 10));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Param", error.PropertyName);
        Assert.Equal("Pagination page must be a positive integer", error.ErrorMessage);
    }

    [Fact]
    public void Validate_WithSizeZero_ReturnsErrorOnParam()
    {
        var result = _validator.Validate(BuildQuery(1, 0));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Param", error.PropertyName);
        Assert.Equal(
            "Pagination size must be a positive integer and less than or equal to 100",
            error.ErrorMessage
        );
    }

    [Fact]
    public void Validate_WithSizeAboveMaximum_ReturnsErrorOnParam()
    {
        var result = _validator.Validate(BuildQuery(1, 101));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Param", error.PropertyName);
        Assert.Equal(
            "Pagination size must be a positive integer and less than or equal to 100",
            error.ErrorMessage
        );
    }

    [Fact]
    public void Validate_WithMaximumSize_ReturnsNoErrors()
    {
        var result = _validator.Validate(BuildQuery(1, 100));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithMinimumSize_ReturnsNoErrors()
    {
        var result = _validator.Validate(BuildQuery(1, 1));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithInvalidPageAndSize_ReturnsTwoErrorsOnParam()
    {
        var result = _validator.Validate(BuildQuery(0, 0));

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.All(result.Errors, x => Assert.Equal("Param", x.PropertyName));
    }

    [Fact]
    public void Validate_WithNegativePageAndOversizedSize_ReturnsTwoErrorsOnParam()
    {
        var result = _validator.Validate(BuildQuery(-5, 500));

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.All(result.Errors, x => Assert.Equal("Param", x.PropertyName));
    }

    [Fact]
    public void Validate_WithValidPageAndInvalidSize_ReturnsOnlyTheSizeError()
    {
        var result = _validator.Validate(BuildQuery(3, 1000));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Param", error.PropertyName);
        Assert.Contains("size", error.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static GetPagedDoctorQuery BuildQuery(int page, int size) =>
        new() { Param = new PaginationParam { Page = page, Size = size } };
}

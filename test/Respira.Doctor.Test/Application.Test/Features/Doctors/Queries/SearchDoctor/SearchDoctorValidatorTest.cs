using Respira.Doctor.Application.Features.Doctors.Queries.SearchDoctor;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Doctor.Application.Test.Features.Doctors.Queries.SearchDoctor;

public sealed class SearchDoctorValidatorTest
{
    private readonly SearchDoctorValidator _validator = new();

    public static TheoryData<string> ValidSearchTerms() =>
        new()
        {
            "nguyen",
            "Nguyen",
            "beta-lactam",
            "nguyen van",
            "12345",
            "a1",
        };

    [Theory]
    [MemberData(nameof(ValidSearchTerms))]
    public void Validate_WithValidSearchTermAndPagination_ReturnsNoErrors(string searchTerm)
    {
        var result = _validator.Validate(BuildQuery(searchTerm, 1, 10));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmptySearchTerm_ReturnsErrorOnSearchTerm()
    {
        var result = _validator.Validate(BuildQuery(string.Empty, 1, 10));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("SearchTerm", error.PropertyName);
        Assert.Equal(
            "Search term must at least have 2 characters to avoid result noise",
            error.ErrorMessage
        );
    }

    [Fact]
    public void Validate_WithSingleCharacterSearchTerm_ReturnsErrorOnSearchTerm()
    {
        var result = _validator.Validate(BuildQuery("a", 1, 10));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("SearchTerm", error.PropertyName);
        Assert.Equal(
            "Search term must at least have 2 characters to avoid result noise",
            error.ErrorMessage
        );
    }

    [Fact]
    public void Validate_WithWhitespaceOnlySearchTerm_ReturnsErrorOnSearchTerm()
    {
        var result = _validator.Validate(BuildQuery("   ", 1, 10));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("SearchTerm", error.PropertyName);
    }

    [Theory]
    [InlineData("a&b")]
    [InlineData("a!b")]
    [InlineData("a(b")]
    [InlineData("a|b")]
    [InlineData("a:b")]
    [InlineData("a*b")]
    [InlineData("a'b")]
    public void Validate_WithInvalidCharactersInSearchTerm_ReturnsErrorOnSearchTerm(
        string searchTerm
    )
    {
        var result = _validator.Validate(BuildQuery(searchTerm, 1, 10));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("SearchTerm", error.PropertyName);
        Assert.Equal(
            "Search term only accept alphanumeric character, and hyphen",
            error.ErrorMessage
        );
    }

    [Fact]
    public void Validate_WithHyphenatedSearchTerm_ReturnsNoErrors()
    {
        var result = _validator.Validate(BuildQuery("beta-lactam", 1, 10));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithWhitespaceInSearchTerm_ReturnsNoErrors()
    {
        var result = _validator.Validate(BuildQuery("nguyen van", 1, 10));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithDigitsInSearchTerm_ReturnsNoErrors()
    {
        var result = _validator.Validate(BuildQuery("001088012345", 1, 10));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithInvalidPage_ReturnsErrorOnParam()
    {
        var result = _validator.Validate(BuildQuery("nguyen", 0, 10));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Param", error.PropertyName);
    }

    [Fact]
    public void Validate_WithInvalidSize_ReturnsErrorOnParam()
    {
        var result = _validator.Validate(BuildQuery("nguyen", 1, 0));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Param", error.PropertyName);
    }

    [Fact]
    public void Validate_WithSizeAboveMaximum_ReturnsErrorOnParam()
    {
        var result = _validator.Validate(BuildQuery("nguyen", 1, 101));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Param", error.PropertyName);
    }

    [Fact]
    public void Validate_WithInvalidSearchTermAndPagination_ReturnsErrorOnBothProperties()
    {
        var result = _validator.Validate(BuildQuery("a", 0, 101));

        Assert.False(result.IsValid);
        Assert.Equal(3, result.Errors.Count);
        Assert.Contains(result.Errors, x => x.PropertyName == "SearchTerm");
        Assert.Equal(2, result.Errors.Count(x => x.PropertyName == "Param"));
    }

    private static SearchDoctorQuery BuildQuery(string searchTerm, int page, int size) =>
        new()
        {
            Param = new PaginationParam { Page = page, Size = size },
            SearchTerm = searchTerm,
        };
}

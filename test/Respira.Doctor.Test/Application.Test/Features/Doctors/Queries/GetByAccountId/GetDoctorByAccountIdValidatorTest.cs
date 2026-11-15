using Respira.Doctor.Application.Features.Doctors.Queries.GetByAccountId;

namespace Respira.Doctor.Application.Test.Features.Doctors.Queries.GetByAccountId;

public sealed class GetDoctorByAccountIdValidatorTest
{
    private readonly GetDoctorByAccountIdValidator _validator = new();

    [Fact]
    public void Validate_WithValidAccountId_ReturnsNoErrors()
    {
        var query = new GetDoctorByAccountIdQuery
        {
            AccountId = Guid.CreateVersion7(),
        };

        var result = _validator.Validate(query);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmptyAccountId_ReturnsError()
    {
        var query = new GetDoctorByAccountIdQuery
        {
            AccountId = Guid.Empty,
        };

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("AccountId", error.PropertyName);
        Assert.Equal("Account ID is required", error.ErrorMessage);
    }
}

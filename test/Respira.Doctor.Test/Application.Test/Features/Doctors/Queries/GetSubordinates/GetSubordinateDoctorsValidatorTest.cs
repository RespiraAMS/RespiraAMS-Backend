using Respira.Doctor.Application.Features.Doctors.Queries.GetSubordinates;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Doctor.Application.Test.Features.Doctors.Queries.GetSubordinates;

public sealed class GetSubordinateDoctorsValidatorTest
{
    private readonly GetSubordinateDoctorsValidator _validator = new();

    [Fact]
    public void Validate_WithValidQuery_ReturnsNoErrors()
    {
        var query = new GetSubordinateDoctorsQuery
        {
            HeadDoctorAccountId = Guid.NewGuid(),
            Param = new PaginationParam { Page = 1, Size = 10 },
        };

        var result = _validator.Validate(query);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmptyHeadDoctorAccountId_ReturnsError()
    {
        var query = new GetSubordinateDoctorsQuery
        {
            HeadDoctorAccountId = Guid.Empty,
            Param = new PaginationParam { Page = 1, Size = 10 },
        };

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(GetSubordinateDoctorsQuery.HeadDoctorAccountId));
    }

    [Fact]
    public void Validate_WithInvalidPagination_ReturnsErrorOnParam()
    {
        var query = new GetSubordinateDoctorsQuery
        {
            HeadDoctorAccountId = Guid.NewGuid(),
            Param = new PaginationParam { Page = 0, Size = 10 },
        };

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Param");
    }
}

using FluentValidation;
using Respira.ServiceDefaults.Extensions;

namespace Application.Features.Doctors.Get.Queries;

/// <summary>
/// Validates <see cref="DoctorListQuery"/>: ensures the shared pagination parameters
/// are within bounds (page &gt; 0, 0 &lt; size &le; 100).
/// </summary>
public class DoctorListQueryValidator : AbstractValidator<DoctorListQuery>
{
    public DoctorListQueryValidator()
    {
        RuleFor(x => x.Param).IsValidPaginationParam();
    }
}

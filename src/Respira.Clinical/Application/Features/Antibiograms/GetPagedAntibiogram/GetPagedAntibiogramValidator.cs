using FluentValidation;
using Respira.ServiceDefaults.Utils.Validators;

namespace Respira.Clinical.Application.Features.Antibiograms.GetPagedAntibiogram
{
    public class GetPagedAntibiogramValidator : AbstractValidator<GetPagedAntibiogramQuery>
    {
        public GetPagedAntibiogramValidator()
        {
            RuleFor(x => x.Param).IsValidPaginationParam();
        }
    }
}

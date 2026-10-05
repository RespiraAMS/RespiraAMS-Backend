using FluentValidation;
using Respira.ServiceDefaults.Utils.Validators;

namespace Respira.Clinical.Application.Features.Pathogens.GetPagedPathogen
{
    public class GetPagedPathogensValidator : AbstractValidator<GetPagedPathogenQuery>
    {
        public GetPagedPathogensValidator()
        {
            RuleFor(x => x.Param).IsValidPaginationParam();
        }
    }
}

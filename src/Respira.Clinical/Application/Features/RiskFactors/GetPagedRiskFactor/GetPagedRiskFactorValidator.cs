using FluentValidation;
using Respira.ServiceDefaults.Utils.Validators;

namespace Respira.Clinical.Application.Features.RiskFactors.GetPagedRiskFactor
{
    public class GetPagedRiskFactorValidator : AbstractValidator<GetPagedRiskFactorQuery>
    {
        public GetPagedRiskFactorValidator()
        {
            RuleFor(x => x.Param).IsValidPaginationParam();
        }
    }
}

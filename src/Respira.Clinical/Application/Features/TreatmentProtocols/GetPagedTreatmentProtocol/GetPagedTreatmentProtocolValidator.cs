using FluentValidation;
using Respira.ServiceDefaults.Utils.Validators;

namespace Respira.Clinical.Application.Features.TreatmentProtocols.GetPagedTreatmentProtocol
{
    public class GetPagedTreatmentProtocolValidator : AbstractValidator<GetPagedTreatmentProtocolQuery>
    {
        public GetPagedTreatmentProtocolValidator()
        {
            RuleFor(x => x.Param).IsValidPaginationParam();
        }
    }
}

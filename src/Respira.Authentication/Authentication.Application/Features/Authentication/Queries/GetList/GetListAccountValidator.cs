using FluentValidation;
using Respira.ServiceDefaults.Utils.Validators;

namespace Respira.Authentication.Application.Features.Authentication.Queries.GetList
{
    /// <summary>
    /// Checks the pagination and filter parameters of the account list.
    /// </summary>
    public class GetListAccountValidator : AbstractValidator<GetListAccountQuery>
    {
        public GetListAccountValidator()
        {
            RuleFor(x => x.PaginationParam).IsValidPaginationParam();

            RuleFor(x => x.Filter!.Role)
                .IsInEnum()
                .When(x => x.Filter?.Role is not null);

            RuleFor(x => x.Filter!.Status)
                .IsInEnum()
                .When(x => x.Filter?.Status is not null);
        }
    }
}

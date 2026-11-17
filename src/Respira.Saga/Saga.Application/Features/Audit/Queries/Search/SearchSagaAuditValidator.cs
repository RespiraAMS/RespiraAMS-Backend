using FluentValidation;
using Respira.ServiceDefaults.Utils.Validators;

namespace Respira.Saga.Application.Features.Audit.Queries.Search;

public sealed class SearchSagaAuditValidator : AbstractValidator<SearchSagaAuditQuery>
{
    public SearchSagaAuditValidator()
    {
        RuleFor(query => query.Param).IsValidPaginationParam();
        RuleFor(query => query)
            .Must(query => !query.From.HasValue || !query.To.HasValue || query.From <= query.To)
            .WithMessage("The 'from' date must be earlier than or equal to the 'to' date.");
    }
}

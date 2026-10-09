using FluentValidation;

namespace Respira.Clinical.Application.Features.Criteria.GetReferencedEntities
{
    public class GetReferencedEntitiesValidator : AbstractValidator<GetReferencedEntitiesQuery>
    {
        public GetReferencedEntitiesValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Criterion ID is required");
        }
    }
}

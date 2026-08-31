namespace Application.Features.Treatments.GetTreatmentById
{
    public class GetTreatmentByIdValidator : AbstractValidator<GetTreatmentByIdQuery>
    {
        public GetTreatmentByIdValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Treatment ID is required");
        }
    }
}

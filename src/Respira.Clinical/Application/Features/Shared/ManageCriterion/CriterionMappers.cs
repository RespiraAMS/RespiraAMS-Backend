using Domain.Enums;

namespace Application.Features.Shared.ManageCriterion;

public class CreateCriterionMapper : ICreateMapper<Criterion, CreateCriterionCommand>
{
    public Criterion ToModel(CreateCriterionCommand command)
    {
        return command.Type switch
        {
            CriterionType.Boolean => new BooleanCriterion() { Name = command.Name },
            CriterionType.Numeric => new NumericCriterion
            {
                Name = command.Name,
                Value = command.Value ?? throw new UnexpectedException("Criterion is numeric type but Value is null")
            },
            _ => throw new UnexpectedException("Unknown criterion type")
        };
    }
}

public class CriterionResultMapper : IResultMapper<Criterion, CriterionItem>
{
    public CriterionItem ToResult(Criterion model)
    {
        return new CriterionItem()
        {
            Id = model.Id,
            Name = model.Name,
            Type = model.Type,
            Value = model.Type == CriterionType.Numeric ? ((NumericCriterion)model).Value : null,
        };
    }
}

public class UpdateCriterionMapper : IUpdateMapper<Criterion, UpdateCriterionCommand>
{
    public void MapModel(Criterion model, UpdateCriterionCommand command)
    {
        if (model.Type != command.Type)
        {
            throw new BadRequestException("Criterion type mismatch: criterion type does not allow for changes");
        }

        model.Name = command.Name;
        switch (model.Type)
        {
            case CriterionType.Boolean:
                break;
            case CriterionType.Numeric:
                ((NumericCriterion)model).Value =
                    command.Value ?? throw new UnexpectedException("Criterion is numeric type but Value is null");
                break;
            default:
                throw new UnexpectedException("Unexpected type for criterion");
        }

        model.UpdatedAt = DateTimeOffset.UtcNow;
        // Criterion type must not change 
    }
}
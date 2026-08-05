using Domain.Enums;
using Range = Domain.Models.Range;

namespace Application.Features.Shared.ManageCriterion;

/*
 * Since Criterion is not a standalone feature (no use case allow directly manipulate Criterion table,
 * all operations are via other entity like IcuHospitalizeCriterion on ResistanceRiskFactor), it's make
 * more sense for Criterion DTOs to be shared DTOs, while entities that associate with Criterion be features,
 * and address the same DTOs
 */

public class CreateCriterionCommand
{
    public required string Name { get; set; }
    public required CriterionType Type { get; set; }
    public Range? Value { get; set; }
}

public class CriterionItem
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required CriterionType Type { get; set; }
    public Range? Value { get; set; }
}

public class UpdateCriterionCommand
{
    public required string Name { get; set; }
    public required CriterionType Type { get; set; }
    public Range? Value { get; set; }
}
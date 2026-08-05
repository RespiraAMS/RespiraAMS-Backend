using Domain.Enums;

namespace Application.Features.Antibiograms.GetPagedAntibiogram;

public class AntibiogramFilter
{
    public Guid? PathogenId { get; set; }
}

public class GetPagedAntibiogramQuery : IQuery
{
    public required PaginationParam Param { get; set; }
    public AntibiogramFilter? Filter { get; set; }
}

public class PathogenResult
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}

public class AntibioticResult
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
}

public class PagedAntibiogramItem
{
    public required Guid Id { get; set; }
    public required PathogenResult Pathogen { get; set; }
    public required MinimumInhibitoryConcentration MicLevel { get; set; }
    public required List<AntibioticResult> Mics { get; set; }
    public required List<AntibioticResult> FirstPriorityMedicines { get; set; }
    public required List<AntibioticResult> SecondPriorityMedicines { get; set; }
}
using Domain.Enums;

namespace Application.Features.Antibiograms.CreateAntibiogram;

public class CreateAntibiogramCommand : ICommand
{
    public required Guid PathogenId { get; set; }
    public required MinimumInhibitoryConcentration MicLevel { get; set; }
    public required List<Guid> MicIds { get; set; }
    public required List<Guid> FirstPriorityMedicineIds { get; set; } = [];
    public required List<Guid> SecondPriorityMedicineIds { get; set; } = [];
}

public class CreateAntibiogramResult(Guid id)
{
    public Guid Id { get; set; } = id;
}
using Domain.Enums;

namespace Application.Features.Antibiograms.UpdateAntibiogram;

public class UpdateAntibiogramCommand : ICommand
{
    public required Guid Id { get; set; }
    public required MinimumInhibitoryConcentration MicLevel { get; set; }
    public required List<Guid> MicIds { get; set; }
    public required List<Guid> FirstPriorityMedicineIds { get; set; } = [];
    public required List<Guid> SecondPriorityMedicineIds { get; set; } = [];
}
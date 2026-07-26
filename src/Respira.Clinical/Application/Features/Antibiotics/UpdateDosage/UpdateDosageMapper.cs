namespace Application.Features.Antibiotics.UpdateDosage;

public class UpdateDosageMapper : IUpdateMapper<Dosage, UpdateDosageCommand>
{
    public void MapModel(Dosage model, UpdateDosageCommand command)
    {
        model.RouteOfAdministration = command.RouteOfAdministration;
        model.Dose = command.Dose;
        model.GlomerularFiltrationRate = command.GlomerularFiltrationRate;
        model.UpdatedAt = DateTimeOffset.UtcNow;
    }
}
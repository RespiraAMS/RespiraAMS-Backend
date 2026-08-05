namespace Application.Features.Antibiotics.AddDosage;

public class AddDosageMapper : ICreateMapper<Dosage, AddDosageCommand>
{
    public Dosage ToModel(AddDosageCommand command)
    {
        return new Dosage
        {
            AntibioticId = command.AntibioticId,
            RouteOfAdministration = command.RouteOfAdministration,
            Dose = command.Dose,
            GlomerularFiltrationRate = command.GlomerularFiltrationRate
        };
    }
}
namespace Application.Features.Antibiotics.CreateAntibiotic;

public class CreateAntibioticMapper : ICreateMapper<Antibiotic, CreateAntibioticCommand>
{
    public Antibiotic ToModel(CreateAntibioticCommand command)
    {
        // Create antibiotic
        var antibiotic = new Antibiotic
        {
            Name = command.Name,
            AntibioticGroupId = command.AntibioticGroupId,
            Classification = command.Classification,
        };

        // Create standard dose
        var standardDose = new Dosage
        {
            AntibioticId = antibiotic.Id,
            RouteOfAdministration = command.RouteOfAdministration,
            Dose = command.StandardDose,
            Crcl = null
        };

        // Add standard dose into antibiotic
        antibiotic.DosageIds.Add(standardDose.Id);
        antibiotic.Dosages.Add(standardDose);
        return antibiotic;
    }
}

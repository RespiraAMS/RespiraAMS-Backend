namespace Application.Features.Antibiotics.CreateAntibiotic;

public class CreateAntibioticMapper : ICreateMapper<Antibiotic, CreateAntibioticCommand>
{
    public Antibiotic ToModel(CreateAntibioticCommand command)
    {
        return new Antibiotic
        {
            Name = command.Name,
            AntibioticGroupId = command.AntibioticGroupId,
            Classification = command.Classification
        };
    }
}

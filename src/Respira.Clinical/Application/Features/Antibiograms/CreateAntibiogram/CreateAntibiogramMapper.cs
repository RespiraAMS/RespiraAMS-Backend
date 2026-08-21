namespace Application.Features.Antibiograms.CreateAntibiogram;

public class CreateAntibiogramMapper : ICreateMapper<Antibiogram, CreateAntibiogramCommand>
{
    public Antibiogram ToModel(CreateAntibiogramCommand command)
    {
        return new Antibiogram
        {
            PathogenId = command.PathogenId,
            MicLevel = command.MicLevel,
        };
    }
}

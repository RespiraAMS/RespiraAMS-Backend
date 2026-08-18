using Domain.Services.Dtos;

namespace Application.Features.Diagnose.TargetedDiagnose;

public class TargetedDiagnoseMapper : IMapper<TargetedDiagnoseQuery, PatientInfo>
{
    public PatientInfo Map(TargetedDiagnoseQuery source)
    {
        return new PatientInfo()
        {
            DateOfBirth = source.DateOfBirth,
            Height = source.Height,
            IsMale = source.IsMale,
            SerumCreatine = source.SerumCreatine,
            Weight = source.Weight,
        };
    }
}

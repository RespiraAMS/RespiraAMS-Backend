using Domain.Services.Dtos;

namespace Application.Features.Diagnose.EmpiricalDiagnose;

public class EmpiricalDiagnosePatientInfoMapper : IMapper<EmpiricalDiagnoseQuery, PatientInfo>
{
    public PatientInfo Map(EmpiricalDiagnoseQuery source)
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

public class EmpiricalDiagnoseClinicalPictureMapper : IMapper<EmpiricalDiagnoseQuery, ClinicalPicture>
{
    public ClinicalPicture Map(EmpiricalDiagnoseQuery source)
    {
        return new ClinicalPicture
        {
            Confusion = source.Confusion,
            Urea = source.Urea,
            Respiratory = source.Respiratory,
            SystolicBloodPressure = source.SystolicBloodPressure,
            DiastolicBloodPressure = source.DiastolicBloodPressure,
            IcuHospitalizeCriteria = source.IcuHospitalizeCriteria,
            ResistanceRiskFactors = source.ResistanceRiskFactors,
            OtherCriteria = source.OtherCriteria
        };
    }
}

using Domain.Services.Dtos;

namespace Application.Features.Diagnose;

public class DiagnoseMapper : ICreateMapper<ClinicalPicture, DiagnoseQuery>
{
    public ClinicalPicture ToModel(DiagnoseQuery command)
    {
        return new ClinicalPicture
        {
            // DateOfBirth = command.DateOfBirth,
            // IsMale = command.IsMale,
            // Weight = command.Weight,
            // SerumCreatine = command.SerumCreatine,
            Confusion = command.Confusion,
            Urea = command.Urea,
            Respiratory = command.Respiratory,
            SystolicBloodPressure = command.SystolicBloodPressure,
            DiastolicBloodPressure = command.DiastolicBloodPressure,
            IcuHospitalizeCriteria = command.IcuHospitalizeCriteria,
            ResistanceRiskFactors = command.ResistanceRiskFactors,
            OtherCriteria = command.OtherCriteria
        };
    }
}

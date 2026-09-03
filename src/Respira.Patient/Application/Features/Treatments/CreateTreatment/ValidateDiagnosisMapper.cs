namespace Application.Features.Treatments.CreateTreatment;

public class ValidateDiagnosisMapper : IMapper<DiagnosisRecord, ValidateDiagnosisQuery>
{
    public ValidateDiagnosisQuery Map(DiagnosisRecord source)
    {
        var antibiotics = source.SystemRecommendedMedicines
            .Concat(source.DoctorChosenMedicines)
            .Select(m => new AntibioticRecord
            {
                Id = m.Id,
                Name = m.Name,
                Classification = m.Classification,
                RouteOfAdministration = m.RouteOfAdministration,
                Dose = m.Dose,
            })
            .ToList();

        if (source is EmpiricalDiagnosisRecord empirical)
        {
            return new ValidateDiagnosisQuery
            {
                Antibiotics = antibiotics,
                Pathogens = empirical.InfectionProbabilityRecords.ConvertAll(p => new PathogenRecord
                {
                    Id = p.Pathogen.Id,
                    Name = p.Pathogen.Name,
                }),
                Severity = empirical.Severity,
                TreatmentSite = empirical.TreatmentSite,
            };
        }

        if (source is TargetedDiagnosisRecord targeted)
        {
            return new ValidateDiagnosisQuery
            {
                Antibiotics = antibiotics,
                Pathogens = [new PathogenRecord
                {
                    Id = targeted.Pathogen.Id,
                    Name = targeted.Pathogen.Name,
                }],
            };
        }

        throw new UnexpectedException($"Invalid diagnosis record: diagnosis record type is not supported: {source.GetType()}");
    }
}

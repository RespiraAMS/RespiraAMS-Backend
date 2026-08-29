using Application.Contracts.Data;
using Domain.Enums;
using JasperFx.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Diagnose.ValidateDiagnosis;

public class ValidateDiagnosisHandler(IDbContext context, ILogger<ValidateDiagnosisHandler> logger) : IQueryHandler<ValidateDiagnosisQuery, ValidateDiagnosisResult>
{
    public async Task<ValidateDiagnosisResult> HandleAsync(ValidateDiagnosisQuery query, CancellationToken cancellationToken = default)
    {
        // Validate if severity and treatment site enums are valid
        if (query.Severity is not null && (!Enum.TryParse<Domain.Enums.Severity>(query.Severity, ignoreCase: true, out var severity) || !Enum.IsDefined(severity)))
        {
            logger.LogInformation("Invalid severity: {severity}", query.Severity);
            return new ValidateDiagnosisResult(false);
        }

        if (query.TreatmentSite is not null && (!Enum.TryParse<TreatmentSite>(query.TreatmentSite, ignoreCase: true, out var treatmentSite) || !Enum.IsDefined(treatmentSite)))
        {
            logger.LogInformation("Invalid treatment site: {treatmentSite}", query.TreatmentSite);
            return new ValidateDiagnosisResult(false);
        }

        // Check if the antibiotic and pathogen exists
        var pathogenIds = query.Pathogens.ConvertAll(p => p.Id);
        var pathogens = await context.Pathogens
            .AsNoTracking()
            .Where(p => pathogenIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p, cancellationToken);
        foreach (var pathogen in query.Pathogens)
        {
            // Check if record exists
            if (!pathogens.TryGetValue(pathogen.Id, out var dbPathogen))
            {
                logger.LogInformation("Pathogen {pathogenId} does not exist", pathogen.Id);
                return new ValidateDiagnosisResult(false);
            }

            // Check if the name match
            if (!dbPathogen.Name.EqualsIgnoreCase(pathogen.Name))
            {
                logger.LogInformation("Pathogen name does not match: {detail}", new
                {
                    DbPathogen = dbPathogen.Name,
                    QueryPathogen = pathogen.Name
                });
                return new ValidateDiagnosisResult(false);
            }
        }

        var antibioticIds = query.Antibiotics.ConvertAll(a => a.Id);
        var antibiotics = await context.Antibiotics
            .AsNoTracking()
            .Include(a => a.Dosages)
            .Where(a => antibioticIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a, cancellationToken);
        foreach (var antibiotic in query.Antibiotics)
        {
            // Check if record exists
            if (!antibiotics.TryGetValue(antibiotic.Id, out var dbAntibiotic))
            {
                logger.LogInformation("Antibiotic {antibioticId} does not exist", antibiotic.Id);
                return new ValidateDiagnosisResult(false);
            }

            // Check if the name match
            if (!dbAntibiotic.Name.EqualsIgnoreCase(antibiotic.Name))
            {
                logger.LogInformation("Antibiotic name does not match: {detail}", new
                {
                    DbAntibiotic = dbAntibiotic.Name,
                    QueryAntibiotic = antibiotic.Name
                });
                return new ValidateDiagnosisResult(false);
            }

            // Check if the route of administration valid and match db record
            if (!Enum.TryParse<RouteOfAdministration>(antibiotic.RouteOfAdministration, ignoreCase: true, out var route) || !Enum.IsDefined(route))
            {
                logger.LogInformation("Invalid route of administration: {detail}", new
                {
                    AntibioticId = antibiotic.Id,
                    QueryRoute = antibiotic.RouteOfAdministration,
                });
                return new ValidateDiagnosisResult(false);
            }

            if (!Enum.TryParse<AwareClassification>(antibiotic.Classification, ignoreCase: true, out var classification) || !Enum.IsDefined(classification))
            {
                logger.LogInformation("Invalid antibiotic classification: {detail}", new
                {
                    AntibioticId = antibiotic.Id,
                    QueryClassification = antibiotic.Classification
                });
                return new ValidateDiagnosisResult(false);
            }

            // Check if the classification match
            if (dbAntibiotic.Classification != classification)
            {
                logger.LogInformation("Antibiotic classification does not match: {detail}", new
                {
                    AntibioticId = antibiotic.Id,
                    DbClassification = dbAntibiotic.Classification,
                    QueryClassification = antibiotic.Classification
                });
                return new ValidateDiagnosisResult(false);
            }

            // Check if the dose exists
            if (!dbAntibiotic.Dosages.Any(d => d.Dose.EqualsIgnoreCase(antibiotic.Dose) && d.RouteOfAdministration == route))
            {
                logger.LogInformation("Antibiotic dose does not exist or route of administration mismatch: {detail}", new
                {
                    DbAntibiotic = dbAntibiotic.Name,
                    QueryAntibiotic = antibiotic.Name
                });
                return new ValidateDiagnosisResult(false);
            }

        }

        return new ValidateDiagnosisResult(true);

    }
}

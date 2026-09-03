using Application.Contracts.Data;
using Domain.Enums;
using JasperFx.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Features.Diagnose.ValidateDiagnosis;

public class ValidateDiagnosisHandler(IDbContext context, ILogger<ValidateDiagnosisHandler> logger)
    : IQueryHandler<ValidateDiagnosisQuery, Result<ValidateDiagnosisResult>>
{
    public async Task<Result<ValidateDiagnosisResult>> HandleAsync(ValidateDiagnosisQuery query,
        CancellationToken cancellationToken = default)
    {
        /*
         * Since this is handler is used for cross service validation, a success result doesn't mean
         * data is valid
         */

        // Validate if severity and treatment site enums are valid
        if (query.Severity is not null &&
            (!Enum.TryParse<Domain.Enums.Severity>(query.Severity, ignoreCase: true, out var severity) ||
             !Enum.IsDefined(severity)))
        {
            logger.LogInformation("Invalid severity: {severity}", query.Severity);
            return Result<ValidateDiagnosisResult>.Success(
                Status.Success,
                new ValidateDiagnosisResult(false, "Invalid severity"));
        }

        if (query.TreatmentSite is not null &&
            (!Enum.TryParse<TreatmentSite>(query.TreatmentSite, ignoreCase: true, out var treatmentSite) ||
             !Enum.IsDefined(treatmentSite)))
        {
            logger.LogInformation("Invalid treatment site: {treatmentSite}", query.TreatmentSite);
            return Result<ValidateDiagnosisResult>.Success(
                Status.Success,
                new ValidateDiagnosisResult(false, "Invalid treatment site"));
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
                return Result<ValidateDiagnosisResult>.Success(
                    Status.Success,
                    new ValidateDiagnosisResult(false, $"Pathogen {pathogen.Id} does not exist"));
            }

            // Check if the name match
            if (!dbPathogen.Name.EqualsIgnoreCase(pathogen.Name))
            {
                logger.LogInformation("Pathogen name does not match: {detail}", new
                {
                    DbPathogen = dbPathogen.Name,
                    QueryPathogen = pathogen.Name
                });
                return Result<ValidateDiagnosisResult>.Success(
                    Status.Success,
                    new ValidateDiagnosisResult(false, $"Pathogen {pathogen.Id} has name mismatch"));
            }
        }

        var antibioticIds = query.Antibiotics.ConvertAll(a => a.Id);
        var antibiotics = await context.Antibiotics
            .AsNoTracking()
            .AsSplitQuery()
            .Include(a => a.Dosages)
            .Where(a => antibioticIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a, cancellationToken);
        foreach (var antibiotic in query.Antibiotics)
        {
            // Check if record exists
            if (!antibiotics.TryGetValue(antibiotic.Id, out var dbAntibiotic))
            {
                logger.LogInformation("Antibiotic {antibioticId} does not exist", antibiotic.Id);
                return Result<ValidateDiagnosisResult>.Success(
                    Status.Success,
                    new ValidateDiagnosisResult(false, $"Pathogen {antibiotic.Id} does not exist"));
            }

            // Check if the name match
            if (!dbAntibiotic.Name.EqualsIgnoreCase(antibiotic.Name))
            {
                logger.LogInformation("Antibiotic name does not match: {detail}", new
                {
                    DbAntibiotic = dbAntibiotic.Name,
                    QueryAntibiotic = antibiotic.Name
                });
                return Result<ValidateDiagnosisResult>.Success(
                    Status.Success,
                    new ValidateDiagnosisResult(false, $"Pathogen {antibiotic.Id} has name mismatch"));
            }

            // Check if the route of administration valid and match db record
            if (!Enum.TryParse<RouteOfAdministration>(antibiotic.RouteOfAdministration, ignoreCase: true,
                    out var route) || !Enum.IsDefined(route))
            {
                logger.LogInformation("Invalid route of administration: {detail}", new
                {
                    AntibioticId = antibiotic.Id,
                    QueryRoute = antibiotic.RouteOfAdministration,
                });
                return Result<ValidateDiagnosisResult>.Success(
                    Status.Success,
                    new ValidateDiagnosisResult(false, "Invalid route of administration"));
            }

            if (!Enum.TryParse<AwareClassification>(antibiotic.Classification, ignoreCase: true,
                    out var classification) || !Enum.IsDefined(classification))
            {
                logger.LogInformation("Invalid antibiotic classification: {detail}", new
                {
                    AntibioticId = antibiotic.Id,
                    QueryClassification = antibiotic.Classification
                });
                return Result<ValidateDiagnosisResult>.Success(
                    Status.Success,
                    new ValidateDiagnosisResult(false, "Invalid antibiotic classification"));
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
                return Result<ValidateDiagnosisResult>.Success(
                    Status.Success,
                    new ValidateDiagnosisResult(false, "Antibiotic classification does not match against db"));
            }

            // Check if the dose exists
            if (!dbAntibiotic.Dosages.Any(d =>
                    d.Dose.EqualsIgnoreCase(antibiotic.Dose) && d.RouteOfAdministration == route))
            {
                logger.LogInformation("Antibiotic dose does not exist or route of administration mismatch: {detail}",
                    new
                    {
                        DbAntibiotic = dbAntibiotic.Name,
                        QueryAntibiotic = antibiotic.Name
                    });
                return Result<ValidateDiagnosisResult>.Success(
                    Status.Success,
                    new ValidateDiagnosisResult(false, "Antibiotic dose or route of administration does not match against db"));
            }
        }

        return Result<ValidateDiagnosisResult>.Success(Status.Success, new ValidateDiagnosisResult(true));
    }
}
using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.CreateClinicalMetric
{
    public class CreateClinicalMetricMapper : ICreateMapper<CreateClinicalMetricCommand, ClinicalMetric>
    {
        public Result<ClinicalMetric> ToModel(CreateClinicalMetricCommand command)
        {
            return Result<ClinicalMetric>.Success(ApplicationStatus.Success, new ClinicalMetric
            {
                Name = command.Name,
                Code = command.Code,
                Description = command.Description,
                Version = command.Version,
                Publisher = command.Publisher,
                IsApplied = true,
                Rules = [],
            });
        }

        public Result<ClinicalMetric> ToModel(CreateClinicalMetricCommand command, object dependencies)
        {
            return ToModel(command);
        }
    }
}

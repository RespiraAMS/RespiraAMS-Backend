using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.CreateClinicalMetric
{
    public class CreateClinicalMetricMapper : ICreateMapper<CreateClinicalMetricCommand, ClinicalMetric>
    {
        public Result<ClinicalMetric> ToModel(CreateClinicalMetricCommand command)
        {
            return Result<ClinicalMetric>.Success(ApplicationStatus.Success, new ClinicalMetric(
                command.Name,
                command.Code,
                command.Description,
                command.Version,
                command.Publisher,
                true,
                []
            ));
        }

        public Result<ClinicalMetric> ToModel(CreateClinicalMetricCommand command, object? dependencies = null)
        {
            return ToModel(command);
        }
    }
}

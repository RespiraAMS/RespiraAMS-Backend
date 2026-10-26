using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Domain.Entities;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Features.ClinicalMetrics.UpdateClinicalMetric
{
    public class UpdateClinicalMetricMapper : IUpdateMapper<ClinicalMetric, UpdateClinicalMetricCommand>
    {
        public Result MapModel(ClinicalMetric model, UpdateClinicalMetricCommand command)
        {
            model.Name = command.Name;
            model.Description = command.Description;
            model.Version = command.Version;
            model.Publisher = command.Publisher;
            model.IsApplied = command.IsApplied;
            model.UpdatedAt = DateTimeOffset.UtcNow;
            return Result.Success(ApplicationStatus.Success);
        }

        public Result MapModel(ClinicalMetric model, UpdateClinicalMetricCommand command, object dependencies)
        {
            return MapModel(model, command);
        }
    }
}

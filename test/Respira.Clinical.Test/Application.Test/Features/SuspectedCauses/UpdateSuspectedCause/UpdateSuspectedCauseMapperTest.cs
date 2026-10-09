using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.SuspectedCauses.UpdateSuspectedCause;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Application.Test.Features.SuspectedCauses.UpdateSuspectedCause
{
    public class UpdateSuspectedCauseMapperTest
    {
        private readonly IUpdateMapper<SuspectedCause, UpdateSuspectedCauseCommand> _mapper = new UpdateSuspectedCauseMapper();

        #region Happy path

        [Fact]
        public void MapModel_UpdatesSeverityAndTreatmentSite_Success()
        {
            var before = DateTimeOffset.UtcNow.AddMinutes(-1);
            var pathogenId = Guid.CreateVersion7();
            var newPathogenId = Guid.CreateVersion7();
            var cause = new SuspectedCause
            {
                PathogenId = pathogenId,
                Severity = Severity.Mild,
                TreatmentSite = TreatmentSite.Outpatient,
            };

            var result = _mapper.MapModel(cause, new UpdateSuspectedCauseCommand
            {
                Id = cause.Id,
                PathogenId = newPathogenId,
                Severity = Severity.Severe,
                TreatmentSite = TreatmentSite.IntensiveCareUnit,
            });
            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(newPathogenId, cause.PathogenId);
            Assert.Equal(Severity.Severe, cause.Severity);
            Assert.Equal(TreatmentSite.IntensiveCareUnit, cause.TreatmentSite);
            Assert.InRange(cause.UpdatedAt, before, DateTimeOffset.UtcNow.AddSeconds(5));

            Assert.Equal(cause.Id, cause.Id);
        }

        #endregion
    }
}

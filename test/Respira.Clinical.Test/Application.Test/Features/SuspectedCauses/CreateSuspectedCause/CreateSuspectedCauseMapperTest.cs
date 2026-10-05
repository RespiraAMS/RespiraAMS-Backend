using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.SuspectedCauses.CreateSuspectedCause;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;

namespace Respira.Application.Test.Features.SuspectedCauses.CreateSuspectedCause
{
    public class CreateSuspectedCauseMapperTest
    {
        private readonly ICreateMapper<SuspectedCause, CreateSuspectedCauseCommand> _mapper = new CreateSuspectedCauseMapper();

        #region Happy path

        [Fact]
        public void ToModel_Success()
        {
            var pathogenId = Guid.CreateVersion7();
            var command = new CreateSuspectedCauseCommand
            {
                PathogenId = pathogenId,
                Severity = Severity.Severe,
                TreatmentSite = TreatmentSite.IntensiveCareUnit,
            };

            var model = _mapper.ToModel(command);

            // Base generates the ID so the handler can return it right after saving
            Assert.NotEqual(Guid.Empty, model.Id);
            Assert.Equal(pathogenId, model.PathogenId);
            Assert.Equal(Severity.Severe, model.Severity);
            Assert.Equal(TreatmentSite.IntensiveCareUnit, model.TreatmentSite);
        }

        #endregion
    }
}

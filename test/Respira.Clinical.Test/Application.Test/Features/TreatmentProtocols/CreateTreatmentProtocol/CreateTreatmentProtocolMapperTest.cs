using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.TreatmentProtocols.CreateTreatmentProtocol;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.TreatmentProtocols.CreateTreatmentProtocol
{
    public class CreateTreatmentProtocolMapperTest
    {
        private readonly ICreateMapper<CreateTreatmentProtocolCommand, TreatmentProtocol> _mapper =
            new CreateTreatmentProtocolMapper();

        #region Happy path

        // Boundary values of the two enum fields: the lowest and highest defined members
        [Theory]
        [InlineData(Severity.Mild, TreatmentSite.Outpatient)]
        [InlineData(Severity.Severe, TreatmentSite.IntensiveCareUnit)]
        public void ToModel_MapsProtocolFields_Success(Severity severity, TreatmentSite treatmentSite)
        {
            var command = new CreateTreatmentProtocolCommand
            {
                Severity = severity,
                TreatmentSite = treatmentSite,
                Medicines = [],
                Pathogens = [],
                Criteria = [],
                Version = "2026.1",
                Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                IsApplied = true,
            };

            var result = _mapper.ToModel(command);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var model = result.Data;

            Assert.Equal(severity, model.Severity);
            Assert.Equal(treatmentSite, model.TreatmentSite);
            Assert.Equal("2026.1", model.Version);
            Assert.Equal("Hiệp hội Lồng ngực Anh (BTS)", model.Publisher);
            Assert.True(model.IsApplied);

            // Base generates the ID so the handler can return it right after saving
            Assert.NotEqual(Guid.Empty, model.Id);

            // Collections stay empty on purpose: the handler fills them through
            // UpdateRelations so EF Core can attach stubs instead of inserting rows
            Assert.Empty(model.Treatments);
            Assert.Empty(model.Pathogens);
            Assert.Empty(model.Criteria);
        }

        // Boundary values of the IsApplied flag
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ToModel_MapsIsApplied_Success(bool isApplied)
        {
            var command = new CreateTreatmentProtocolCommand
            {
                Severity = Severity.Moderate,
                TreatmentSite = TreatmentSite.Inpatient,
                Medicines = [],
                Pathogens = [],
                Criteria = [],
                Version = "2002-2003",
                Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                IsApplied = isApplied,
            };

            var result = _mapper.ToModel(command);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);
            Assert.Equal(isApplied, result.Data.IsApplied);
        }

        [Fact]
        public void ToModel_WithDependencies_Success()
        {
            // The dependency overload is used by the generic handler pipeline and must
            // produce the same model as the plain overload
            var command = new CreateTreatmentProtocolCommand
            {
                Severity = Severity.Severe,
                TreatmentSite = TreatmentSite.IntensiveCareUnit,
                Medicines = [[Guid.CreateVersion7()]],
                Pathogens = [Guid.CreateVersion7()],
                Criteria = [Guid.CreateVersion7()],
                Version = "2026.1",
                Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                IsApplied = false,
            };

            var result = _mapper.ToModel(command, new object());

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.Equal(command.Severity, result.Data.Severity);
            Assert.Equal(command.TreatmentSite, result.Data.TreatmentSite);
            Assert.Equal(command.Version, result.Data.Version);
            Assert.Equal(command.Publisher, result.Data.Publisher);
            Assert.Equal(command.IsApplied, result.Data.IsApplied);
            Assert.Empty(result.Data.Treatments);
            Assert.Empty(result.Data.Pathogens);
            Assert.Empty(result.Data.Criteria);
        }

        #endregion
    }
}

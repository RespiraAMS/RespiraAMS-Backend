using Respira.Clinical.Application.Contracts.Mappers;
using Respira.Clinical.Application.Features.TreatmentProtocols.UpdateTreatmentProtocol;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Test.Features.TreatmentProtocols.UpdateTreatmentProtocol
{
    public class UpdateTreatmentProtocolMapperTest
    {
        private readonly IUpdateMapper<TreatmentProtocol, UpdateTreatmentProtocolCommand> _mapper =
            new UpdateTreatmentProtocolMapper();

        #region Happy path

        // Boundary values of the two enum fields: the lowest and highest defined members
        [Theory]
        [InlineData(Severity.Mild, TreatmentSite.Outpatient)]
        [InlineData(Severity.Severe, TreatmentSite.IntensiveCareUnit)]
        public void MapModel_UpdatesProtocolFields_Success(
            Severity severity, TreatmentSite treatmentSite)
        {
            var protocol = new TreatmentProtocol
            {
                Severity = Severity.Moderate,
                TreatmentSite = TreatmentSite.Inpatient,
                Version = "2024.1",
                Publisher = "Bộ Y tế Việt Nam",
                IsApplied = false,
                UpdatedAt = new DateTimeOffset(2025, 11, 20, 8, 15, 0, TimeSpan.Zero),
            };
            var previousUpdatedAt = protocol.UpdatedAt;

            var command = new UpdateTreatmentProtocolCommand
            {
                Id = protocol.Id,
                Severity = severity,
                TreatmentSite = treatmentSite,
                Medicines = [],
                Pathogens = [],
                Criteria = [],
                Version = "2026.1",
                Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                IsApplied = true,
            };

            var result = _mapper.MapModel(protocol, command);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(severity, protocol.Severity);
            Assert.Equal(treatmentSite, protocol.TreatmentSite);
            Assert.Equal("2026.1", protocol.Version);
            Assert.Equal("Hiệp hội Lồng ngực Anh (BTS)", protocol.Publisher);
            Assert.True(protocol.IsApplied);

            // The mapper must stamp the modification time so the audit trail reflects it
            Assert.True(protocol.UpdatedAt > previousUpdatedAt);
        }

        // Boundary values of the IsApplied flag
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void MapModel_MapsIsApplied_Success(bool isApplied)
        {
            var protocol = new TreatmentProtocol
            {
                Severity = Severity.Moderate,
                TreatmentSite = TreatmentSite.Inpatient,
                Version = "2024.1",
                Publisher = "Bộ Y tế Việt Nam",
                IsApplied = isApplied,
            };
            var command = new UpdateTreatmentProtocolCommand
            {
                Id = protocol.Id,
                Severity = Severity.Moderate,
                TreatmentSite = TreatmentSite.Inpatient,
                Medicines = [],
                Pathogens = [],
                Criteria = [],
                Version = "2024.1",
                Publisher = "Bộ Y tế Việt Nam",
                IsApplied = isApplied,
            };

            var result = _mapper.MapModel(protocol, command);

            Assert.True(result.IsSuccess());
            Assert.Equal(isApplied, protocol.IsApplied);
        }

        [Fact]
        public void MapModel_LeavesRelationsUnchanged_Success()
        {
            // Relations are replaced by the handler through the join-table update
            // helpers, so the mapper must not touch the navigation collections
            var pathogen = new Pathogen
            {
                Name = "Streptococcus pneumoniae",
                Description = "Gram-positive diplococcus",
                IsAtypical = false,
            };
            var criterion = new Criterion
            {
                Name = "Patient is an adult",
                Formula = new BooleanConstantFormula(true),
            };
            var treatment = new Treatment
            {
                TreatmentProtocolId = Guid.CreateVersion7(),
            };
            var protocol = new TreatmentProtocol
            {
                Severity = Severity.Mild,
                TreatmentSite = TreatmentSite.Outpatient,
                Version = "2024.1",
                Publisher = "Bộ Y tế Việt Nam",
                IsApplied = true,
                Pathogens = [pathogen],
                Criteria = [criterion],
                Treatments = [treatment],
            };

            var command = new UpdateTreatmentProtocolCommand
            {
                Id = protocol.Id,
                Severity = Severity.Severe,
                TreatmentSite = TreatmentSite.IntensiveCareUnit,
                Medicines = [],
                Pathogens = [],
                Criteria = [],
                Version = "2026.1",
                Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                IsApplied = false,
            };

            var result = _mapper.MapModel(protocol, command);

            Assert.True(result.IsSuccess());
            Assert.Same(pathogen, Assert.Single(protocol.Pathogens));
            Assert.Same(criterion, Assert.Single(protocol.Criteria));
            Assert.Same(treatment, Assert.Single(protocol.Treatments));
        }

        [Fact]
        public void MapModel_WithDependencies_Success()
        {
            // The dependency overload is used by the generic handler pipeline and must
            // produce the same update as the plain overload
            var protocol = new TreatmentProtocol
            {
                Severity = Severity.Mild,
                TreatmentSite = TreatmentSite.Outpatient,
                Version = "2024.1",
                Publisher = "Bộ Y tế Việt Nam",
                IsApplied = false,
            };
            var command = new UpdateTreatmentProtocolCommand
            {
                Id = protocol.Id,
                Severity = Severity.Severe,
                TreatmentSite = TreatmentSite.IntensiveCareUnit,
                Medicines = [[Guid.CreateVersion7()]],
                Pathogens = [Guid.CreateVersion7()],
                Criteria = [Guid.CreateVersion7()],
                Version = "2026.1",
                Publisher = "Hiệp hội Lồng ngực Anh (BTS)",
                IsApplied = true,
            };

            var result = _mapper.MapModel(protocol, command, new object());

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.Equal(command.Severity, protocol.Severity);
            Assert.Equal(command.TreatmentSite, protocol.TreatmentSite);
            Assert.Equal(command.Version, protocol.Version);
            Assert.Equal(command.Publisher, protocol.Publisher);
            Assert.Equal(command.IsApplied, protocol.IsApplied);
        }

        #endregion
    }
}

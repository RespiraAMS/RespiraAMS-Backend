using Respira.Clinical.Application.Features.TreatmentProtocols.UpdateTreatmentProtocol;
using Respira.Clinical.Domain.Enums;

namespace Respira.Clinical.Application.Test.Features.TreatmentProtocols.UpdateTreatmentProtocol
{
    public class UpdateTreatmentProtocolValidatorTest
    {
        private readonly UpdateTreatmentProtocolValidator _validator = new();

        #region Valid command

        [Fact]
        public async Task UpdateTreatmentProtocol_MinimalValues_Success()
        {
            // Boundary: the smallest accepted command has exactly 1 medicine composition
            // with exactly 1 antibiotic, and no pathogens or criteria at all (0 items)
            var command = CreateCommand(medicines: [[Guid.CreateVersion7()]]);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task UpdateTreatmentProtocol_FullValues_Success()
        {
            var command = CreateCommand(
                medicines:
                [
                    [Guid.CreateVersion7()],
                    [Guid.CreateVersion7(), Guid.CreateVersion7()],
                ],
                pathogens: [Guid.CreateVersion7(), Guid.CreateVersion7()],
                criteria: [Guid.CreateVersion7(), Guid.CreateVersion7()],
                version: "2002-2003",
                publisher: "Hiệp hội Lồng ngực Anh (BTS)");

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // Boundary values of the enums: only the lowest and highest defined members are valid
        [Theory]
        [InlineData(Severity.Mild, TreatmentSite.Outpatient)]
        [InlineData(Severity.Severe, TreatmentSite.IntensiveCareUnit)]
        public async Task UpdateTreatmentProtocol_EnumBoundaries_Success(
            Severity severity, TreatmentSite treatmentSite)
        {
            var command = CreateCommand(medicines: [[Guid.CreateVersion7()]]) with
            {
                Severity = severity,
                TreatmentSite = treatmentSite,
            };

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // Boundary: a UUID v7 is the usual shape, plus the lowest non-empty GUID
        // value that NotEmpty still accepts
        [Theory]
        [MemberData(nameof(ValidIds))]
        public async Task UpdateTreatmentProtocol_ValidId_Success(Guid id)
        {
            var command = CreateCommand(medicines: [[Guid.CreateVersion7()]]) with
            {
                Id = id,
            };

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        public static readonly TheoryData<Guid> ValidIds =
        [
            Guid.CreateVersion7(),
            new Guid("00000000-0000-0000-0000-000000000001"),
            new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff"),
        ];

        [Fact]
        public async Task UpdateTreatmentProtocol_SingleCharacterVersionAndPublisher_Success()
        {
            // Boundary: a single character is the shortest value NotEmpty accepts
            var command = CreateCommand(medicines: [[Guid.CreateVersion7()]]) with
            {
                Version = "1",
                Publisher = "P",
            };

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid command

        [Fact]
        public async Task UpdateTreatmentProtocol_EmptyId_Fail()
        {
            // Boundary: Guid.Empty is the only value rejected by NotEmpty on a Guid
            var command = CreateCommand(medicines: [[Guid.CreateVersion7()]]) with
            {
                Id = Guid.Empty,
            };

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Id", result.Errors[0].PropertyName);
        }

        // Enum raw values: 0 is outside Severity (its lowest member is 1) and 4 is one
        // past the highest member (3), so both are the boundaries of the invalid range
        [Theory]
        [InlineData(0, "Severity")]
        [InlineData(4, "Severity")]
        public async Task UpdateTreatmentProtocol_InvalidSeverity_Fail(int severity, string property)
        {
            var command = CreateCommand(medicines: [[Guid.CreateVersion7()]]) with
            {
                Severity = (Severity)severity,
            };

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal(property, result.Errors[0].PropertyName);
        }

        // TreatmentSite members are 0, 1 and 2, so -1 and 3 are the adjacent invalid values
        [Theory]
        [InlineData(-1, "TreatmentSite")]
        [InlineData(3, "TreatmentSite")]
        public async Task UpdateTreatmentProtocol_InvalidTreatmentSite_Fail(
            int treatmentSite, string property)
        {
            var command = CreateCommand(medicines: [[Guid.CreateVersion7()]]) with
            {
                TreatmentSite = (TreatmentSite)treatmentSite,
            };

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal(property, result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateTreatmentProtocol_NoMedicineComposition_Fail()
        {
            // Boundary: 0 compositions, one below the minimum of 1
            var command = CreateCommand(medicines: []);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Medicines", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateTreatmentProtocol_EmptyMedicineComposition_Fail()
        {
            // Boundary: a composition with 0 antibiotics, one below the minimum of 1
            var command = CreateCommand(medicines: [[]]);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Medicines[0]", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateTreatmentProtocol_EmptyAntibioticId_Fail()
        {
            // Boundary: Guid.Empty is the only UUID value rejected by the rule
            var command = CreateCommand(medicines: [[Guid.Empty]]);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Medicines[0]", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateTreatmentProtocol_EmptyPathogenId_Fail()
        {
            var command = CreateCommand(
                medicines: [[Guid.CreateVersion7()]],
                pathogens: [Guid.Empty]);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Pathogens[0]", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateTreatmentProtocol_EmptyCriterionId_Fail()
        {
            var command = CreateCommand(
                medicines: [[Guid.CreateVersion7()]],
                criteria: [Guid.Empty]);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Criteria[0]", result.Errors[0].PropertyName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UpdateTreatmentProtocol_EmptyVersion_Fail(string version)
        {
            var command = CreateCommand(medicines: [[Guid.CreateVersion7()]]) with
            {
                Version = version,
            };

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Version", result.Errors[0].PropertyName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task UpdateTreatmentProtocol_EmptyPublisher_Fail(string publisher)
        {
            var command = CreateCommand(medicines: [[Guid.CreateVersion7()]]) with
            {
                Publisher = publisher,
            };

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Publisher", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task UpdateTreatmentProtocol_AllFieldsInvalid_Fail()
        {
            var command = new UpdateTreatmentProtocolCommand
            {
                Id = Guid.Empty,
                Severity = 0,
                TreatmentSite = (TreatmentSite)3,
                Medicines = [],
                Pathogens = [],
                Criteria = [],
                Version = "",
                Publisher = "",
                IsApplied = false,
            };

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Equal(6, result.Errors.Count);
            Assert.Contains(result.Errors, x => x.PropertyName == "Id");
            Assert.Contains(result.Errors, x => x.PropertyName == "Severity");
            Assert.Contains(result.Errors, x => x.PropertyName == "TreatmentSite");
            Assert.Contains(result.Errors, x => x.PropertyName == "Medicines");
            Assert.Contains(result.Errors, x => x.PropertyName == "Version");
            Assert.Contains(result.Errors, x => x.PropertyName == "Publisher");
        }

        [Fact]
        public async Task UpdateTreatmentProtocol_MultipleEmptyAntibioticIds_Fail()
        {
            // RuleForEach reports one error per offending composition
            var command = CreateCommand(
                medicines:
                [
                    [Guid.Empty],
                    [Guid.CreateVersion7()],
                    [Guid.Empty, Guid.CreateVersion7()],
                ]);

            var result = await _validator.ValidateAsync(
                command, TestContext.Current.CancellationToken);

            Assert.Equal(2, result.Errors.Count);
            Assert.Contains(result.Errors, x => x.PropertyName == "Medicines[0]");
            Assert.Contains(result.Errors, x => x.PropertyName == "Medicines[2]");
        }

        #endregion

        private static UpdateTreatmentProtocolCommand CreateCommand(
            List<List<Guid>>? medicines = null,
            List<Guid>? pathogens = null,
            List<Guid>? criteria = null,
            string version = "2026.1",
            string publisher = "Hiệp hội Lồng ngực Anh (BTS)")
        {
            return new UpdateTreatmentProtocolCommand
            {
                Id = Guid.CreateVersion7(),
                Severity = Severity.Moderate,
                TreatmentSite = TreatmentSite.Inpatient,
                Medicines = medicines ?? [[Guid.CreateVersion7()]],
                Pathogens = pathogens ?? [],
                Criteria = criteria ?? [],
                Version = version,
                Publisher = publisher,
                IsApplied = true,
            };
        }
    }
}

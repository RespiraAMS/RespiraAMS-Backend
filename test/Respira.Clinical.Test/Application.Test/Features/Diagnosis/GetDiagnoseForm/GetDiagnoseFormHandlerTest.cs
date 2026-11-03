using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.Diagnosis.GetDiagnoseForm;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Clinical.Application.Test.Features.Diagnosis.GetDiagnoseForm
{
    public class GetDiagnoseFormHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly GetDiagnosisFormHandler _handler;
        private readonly IDbContext _context;

        public GetDiagnoseFormHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>()
                .UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            var logger = new Mock<ILogger<GetDiagnosisFormHandler>>().Object;

            // Initialize handler
            _handler = new(_context, logger);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        public async ValueTask InitializeAsync()
        {
            // Clear leftover data (children first for FK constraints) so the count
            // assertions are deterministic across runs. IgnoreQueryFilters is needed
            // because soft deleted rows are hidden by the query filter but still
            // occupy the table
            await _context.Dosages.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Antibiotics.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            // A group can reference its parent, so delete sub-groups before parents
            await _context.AntibioticGroups.IgnoreQueryFilters()
                .Where(x => x.ParentId != null)
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.AntibioticGroups.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.Pathogens.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.ClinicalVariables.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        #region Seed helpers

        /*
         * Ids are fixed and assigned so that primary-key order is deliberately different
         * from name order. This makes an ordering bug visible instead of accidentally
         * passing because insertion order and name order happen to match.
         */
        private static Guid Id(int sequence) =>
            Guid.Parse($"0192f5c0-0000-7000-8000-{sequence:D12}");

        private static Range NewRange(
            decimal min,
            decimal max,
            string? unit,
            bool minExclusive = false,
            bool maxExclusive = false)
        {
            return new Range
            {
                Min = min,
                Max = max,
                Unit = unit,
                IsMinExclusive = minExclusive,
                IsMaxExclusive = maxExclusive,
            };
        }

        private static NumericClinicalVariable Numeric(
            string name,
            string code,
            string description,
            ClinicalVariableCategory category,
            string? unit,
            bool isRequired,
            Range acceptedRange,
            Range? normalRange = null)
        {
            return new NumericClinicalVariable
            {
                Code = code,
                Name = name,
                Description = description,
                CanonicalUnit = unit,
                IsRequired = isRequired,
                Category = category,
                AcceptedRange = acceptedRange,
                NormalRange = normalRange,
            };
        }

        private static BooleanClinicalVariable Boolean(
            string name,
            string code,
            string description,
            ClinicalVariableCategory category,
            bool isRequired,
            bool softDeleted = false)
        {
            return new BooleanClinicalVariable
            {
                Code = code,
                Name = name,
                Description = description,
                IsRequired = isRequired,
                Category = category,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? DateTimeOffset.UtcNow : null,
            };
        }

        private static CategoricalClinicalVariable Categorical(
            string name,
            string code,
            string description,
            ClinicalVariableCategory category,
            bool isRequired,
            List<string> acceptedValues,
            bool softDeleted = false)
        {
            return new CategoricalClinicalVariable
            {
                Code = code,
                Name = name,
                Description = description,
                IsRequired = isRequired,
                Category = category,
                AcceptedValues = acceptedValues,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? DateTimeOffset.UtcNow : null,
            };
        }

        private static AntibioticGroup Group(string name)
        {
            return new AntibioticGroup
            {
                Name = name,
                Description = $"{name} antibiotic group",
                ParentId = null,
            };
        }

        private static Antibiotic Antibiotic(
            string name,
            Guid groupId,
            AwareClassification classification,
            Guid id,
            bool softDeleted = false)
        {
            return new Antibiotic
            {
                Id = id,
                Name = name,
                AntibioticGroupId = groupId,
                Classification = classification,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? DateTimeOffset.UtcNow : null,
            };
        }

        private static Pathogen Pathogen(
            string name,
            bool isAtypical,
            Guid id,
            bool softDeleted = false)
        {
            return new Pathogen
            {
                Id = id,
                Name = name,
                Description = $"{name} pathogen",
                IsAtypical = isAtypical,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? DateTimeOffset.UtcNow : null,
            };
        }

        /*
         * Seed order differs from category / value type order and from name order, so
         * both the clinical variable ordering rule and the reference list ordering rule
         * (if implemented) are exercised.
         */
        private async Task SeedFormAsync()
        {
            var betaLactams = Group("Beta-lactams");
            var macrolides = Group("Macrolides");
            var carbapenems = Group("Carbapenems");

            await _context.ClinicalVariables.AddRangeAsync(
            [
                Numeric(
                    "Blood glucose",
                    "2339-0",
                    "Blood glucose mass per volume in serum or plasma",
                    ClinicalVariableCategory.Paraclinical,
                    "mg/dL",
                    isRequired: true,
                    acceptedRange: NewRange(20, 800, "mg/dL"),
                    normalRange: NewRange(70, 99, "mg/dL")),
                Boolean(
                    "Female sex",
                    "46098-0",
                    "Whether the patient is female",
                    ClinicalVariableCategory.PersonalInformation,
                    isRequired: true),
                Categorical(
                    "Pregnancy status",
                    "82810-3",
                    "Pregnancy status of the patient",
                    ClinicalVariableCategory.PersonalInformation,
                    isRequired: false,
                    acceptedValues: ["PREGNANT", "NOT_PREGNANT", "LACTATING"]),
                Boolean(
                    "Current smoker",
                    "72166-2",
                    "Whether the patient currently smokes",
                    ClinicalVariableCategory.MedicalHistory,
                    isRequired: false),
                Categorical(
                    "SARS-CoV-2 RNA",
                    "94500-6",
                    "SARS-CoV-2 RNA detected in respiratory specimen",
                    ClinicalVariableCategory.Paraclinical,
                    isRequired: false,
                    acceptedValues: ["DETECTED", "NOT_DETECTED"]),
                Numeric(
                    "Body temperature",
                    "8310-5",
                    "Body temperature measured at the axilla",
                    ClinicalVariableCategory.Clinical,
                    "Cel",
                    isRequired: true,
                    acceptedRange: NewRange(34.0m, 42.0m, "Cel"),
                    normalRange: NewRange(36.1m, 37.2m, "Cel")),
            ], TestContext.Current.CancellationToken);

            await _context.AntibioticGroups.AddRangeAsync(
                [betaLactams, macrolides, carbapenems],
                TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            await _context.Antibiotics.AddRangeAsync(
            [
                Antibiotic("Meropenem", carbapenems.Id, AwareClassification.Reserve, Id(1)),
                Antibiotic("Amoxicillin", betaLactams.Id, AwareClassification.Access, Id(2)),
                Antibiotic("Azithromycin", macrolides.Id, AwareClassification.Watch, Id(3)),
            ], TestContext.Current.CancellationToken);

            await _context.Pathogens.AddRangeAsync(
            [
                Pathogen("Pseudomonas aeruginosa", isAtypical: false, Id(4)),
                Pathogen("Streptococcus pneumoniae", isAtypical: false, Id(5)),
                Pathogen("Mycoplasma pneumoniae", isAtypical: true, Id(6)),
            ], TestContext.Current.CancellationToken);

            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        #endregion

        #region Happy path

        [Fact]
        public async Task GetDiagnoseForm_AllData_ReturnsCompleteForm_Success()
        {
            await SeedFormAsync();

            var result = await _handler.HandleAsync(
                new GetDiagnoseFormQuery(), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            var variables = result.Data.ClinicalVariables.ToList();
            Assert.Equal(6, variables.Count);

            // Categorical variable projects its accepted values and no numeric ranges
            var sars = Assert.Single(variables, x => x.Code == "94500-6");
            Assert.NotEqual(Guid.Empty, sars.Id);
            Assert.Equal("SARS-CoV-2 RNA", sars.Name);
            Assert.Equal("SARS-CoV-2 RNA detected in respiratory specimen", sars.Description);
            Assert.Equal(ClinicalValueType.Categorical, sars.ValueType);
            Assert.Equal(ClinicalVariableCategory.Paraclinical, sars.Category);
            Assert.False(sars.IsRequired);
            Assert.Null(sars.CanonicalUnit);
            Assert.Null(sars.NormalRange);
            Assert.Null(sars.AcceptedRange);
            Assert.Equal(["DETECTED", "NOT_DETECTED"], sars.AcceptedValues);

            // Numeric variable projects both ranges and no accepted values
            var glucose = Assert.Single(variables, x => x.Code == "2339-0");
            Assert.Equal(ClinicalValueType.Numeric, glucose.ValueType);
            Assert.Equal(ClinicalVariableCategory.Paraclinical, glucose.Category);
            Assert.True(glucose.IsRequired);
            Assert.Equal("mg/dL", glucose.CanonicalUnit);
            Assert.Null(glucose.AcceptedValues);
            Assert.NotNull(glucose.AcceptedRange);
            Assert.Equal(20m, glucose.AcceptedRange.Min);
            Assert.Equal(800m, glucose.AcceptedRange.Max);
            Assert.False(glucose.AcceptedRange.IsMinExclusive);
            Assert.False(glucose.AcceptedRange.IsMaxExclusive);
            Assert.Equal("mg/dL", glucose.AcceptedRange.Unit);
            Assert.NotNull(glucose.NormalRange);
            Assert.Equal(70m, glucose.NormalRange.Min);
            Assert.Equal(99m, glucose.NormalRange.Max);
            Assert.Equal("mg/dL", glucose.NormalRange.Unit);

            // Boolean variable has neither accepted values nor numeric ranges
            var female = Assert.Single(variables, x => x.Code == "46098-0");
            Assert.Equal(ClinicalValueType.Boolean, female.ValueType);
            Assert.Equal(ClinicalVariableCategory.PersonalInformation, female.Category);
            Assert.True(female.IsRequired);
            Assert.Null(female.CanonicalUnit);
            Assert.Null(female.AcceptedValues);
            Assert.Null(female.NormalRange);
            Assert.Null(female.AcceptedRange);

            var antibiotics = result.Data.Antibiotics.ToList();
            Assert.Equal(3, antibiotics.Count);

            // The antibiotic projection must resolve the group name and keep AWaRe
            // classification together with the antibiotic identity
            var amoxicillin = Assert.Single(antibiotics, x => x.Name == "Amoxicillin");
            Assert.NotEqual(Guid.Empty, amoxicillin.Id);
            Assert.NotEqual(Guid.Empty, amoxicillin.AntibioticGroupId);
            Assert.Equal("Beta-lactams", amoxicillin.AntibioticGroupName);
            Assert.Equal(AwareClassification.Access, amoxicillin.Classification);

            var azithromycin = Assert.Single(antibiotics, x => x.Name == "Azithromycin");
            Assert.Equal("Macrolides", azithromycin.AntibioticGroupName);
            Assert.Equal(AwareClassification.Watch, azithromycin.Classification);

            var meropenem = Assert.Single(antibiotics, x => x.Name == "Meropenem");
            Assert.Equal("Carbapenems", meropenem.AntibioticGroupName);
            Assert.Equal(AwareClassification.Reserve, meropenem.Classification);

            var pathogens = result.Data.Pathogens.ToList();
            Assert.Equal(3, pathogens.Count);
            Assert.All(pathogens, x => Assert.NotEqual(Guid.Empty, x.Id));
            Assert.Contains(pathogens, x => x.Name == "Pseudomonas aeruginosa");
            Assert.Contains(pathogens, x => x.Name == "Streptococcus pneumoniae");
            Assert.Contains(pathogens, x => x.Name == "Mycoplasma pneumoniae");
        }

        [Fact]
        public async Task GetDiagnoseForm_ClinicalVariables_OrderedByCategoryThenValueType_Success()
        {
            await SeedFormAsync();

            var result = await _handler.HandleAsync(
                new GetDiagnoseFormQuery(), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            /*
             * Business rule: the form presents clinical variables grouped by category
             * (personal information, medical history, paraclinical, clinical) and, inside
             * a category, numeric inputs come before boolean and categorical inputs.
             */
            Assert.Equal(
            [
                "Female sex",        // PersonalInformation / Boolean
                "Pregnancy status",  // PersonalInformation / Categorical
                "Current smoker",    // MedicalHistory / Boolean
                "Blood glucose",     // Paraclinical / Numeric
                "SARS-CoV-2 RNA",    // Paraclinical / Categorical
                "Body temperature",  // Clinical / Numeric
            ], [.. result.Data.ClinicalVariables.Select(x => x.Name)]);
        }

        [Fact]
        public async Task GetDiagnoseForm_Antibiotics_OrderedByNameAscending_Success()
        {
            await SeedFormAsync();

            var result = await _handler.HandleAsync(
                new GetDiagnoseFormQuery(), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            // Business rule: reference pick lists are alphabetical so clinicians can
            // find an item quickly (the sibling GetAntibiotics query orders by name)
            Assert.Equal(
            [
                "Amoxicillin",
                "Azithromycin",
                "Meropenem",
            ], [.. result.Data.Antibiotics.Select(x => x.Name)]);
        }

        [Fact]
        public async Task GetDiagnoseForm_Pathogens_OrderedByNameAscending_Success()
        {
            await SeedFormAsync();

            var result = await _handler.HandleAsync(
                new GetDiagnoseFormQuery(), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            // Business rule: pathogen options are alphabetical so clinicians can find
            // an item quickly (the sibling GetPathogens query orders by name)
            Assert.Equal(
            [
                "Mycoplasma pneumoniae",
                "Pseudomonas aeruginosa",
                "Streptococcus pneumoniae",
            ], [.. result.Data.Pathogens.Select(x => x.Name)]);
        }

        [Fact]
        public async Task GetDiagnoseForm_ExcludesSoftDeletedData_Success()
        {
            var betaLactams = Group("Beta-lactams");
            await _context.AntibioticGroups.AddAsync(
                betaLactams, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            await _context.ClinicalVariables.AddRangeAsync(
            [
                Boolean(
                    "Female sex",
                    "46098-0",
                    "Whether the patient is female",
                    ClinicalVariableCategory.PersonalInformation,
                    isRequired: true),
                Boolean(
                    "Current smoker",
                    "72166-2",
                    "Whether the patient currently smokes",
                    ClinicalVariableCategory.MedicalHistory,
                    isRequired: false,
                    softDeleted: true),
            ], TestContext.Current.CancellationToken);

            await _context.Antibiotics.AddRangeAsync(
            [
                Antibiotic("Amoxicillin", betaLactams.Id, AwareClassification.Access, Id(1)),
                Antibiotic(
                    "Meropenem",
                    betaLactams.Id,
                    AwareClassification.Reserve,
                    Id(2),
                    softDeleted: true),
            ], TestContext.Current.CancellationToken);

            await _context.Pathogens.AddRangeAsync(
            [
                Pathogen("Streptococcus pneumoniae", isAtypical: false, Id(3)),
                Pathogen(
                    "Mycoplasma pneumoniae",
                    isAtypical: true,
                    Id(4),
                    softDeleted: true),
            ], TestContext.Current.CancellationToken);

            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _handler.HandleAsync(
                new GetDiagnoseFormQuery(), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            // Business rule: deleted reference data must never be offered on the form
            var variable = Assert.Single(result.Data.ClinicalVariables);
            Assert.Equal("Female sex", variable.Name);

            var antibiotic = Assert.Single(result.Data.Antibiotics);
            Assert.Equal("Amoxicillin", antibiotic.Name);

            var pathogen = Assert.Single(result.Data.Pathogens);
            Assert.Equal("Streptococcus pneumoniae", pathogen.Name);
        }

        [Fact]
        public async Task GetDiagnoseForm_SingleClinicalVariable_ReturnsEmptyReferenceLists_Success()
        {
            // Lower boundary: one clinical variable (the minimum the form can show)
            // and no antibiotic / pathogen reference data configured yet
            await _context.ClinicalVariables.AddAsync(
                Boolean(
                    "Female sex",
                    "46098-0",
                    "Whether the patient is female",
                    ClinicalVariableCategory.PersonalInformation,
                    isRequired: true),
                TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _handler.HandleAsync(
                new GetDiagnoseFormQuery(), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Single(result.Data.ClinicalVariables);
            Assert.Empty(result.Data.Antibiotics);
            Assert.Empty(result.Data.Pathogens);
        }

        [Fact]
        public async Task GetDiagnoseForm_NumericRangeBoundaries_AreProjectedUnchanged_Success()
        {
            /*
             * Boundary values of the numeric ranges: the lower and upper limits of both
             * the accepted and the normal range, in inclusive and exclusive form, must
             * survive the projection unchanged so the client can render and validate the
             * exact same boundaries the engine uses.
             */
            await _context.ClinicalVariables.AddRangeAsync(
            [
                Numeric(
                    "Age",
                    "30525-0",
                    "Age at the time of diagnosis",
                    ClinicalVariableCategory.PersonalInformation,
                    "a",
                    isRequired: true,
                    acceptedRange: NewRange(0, 120, "a", minExclusive: true, maxExclusive: true)),
                Numeric(
                    "Blood glucose",
                    "2339-0",
                    "Blood glucose mass per volume in serum or plasma",
                    ClinicalVariableCategory.Paraclinical,
                    "mg/dL",
                    isRequired: true,
                    acceptedRange: NewRange(20, 800, "mg/dL"),
                    normalRange: NewRange(70, 99, "mg/dL")),
                Numeric(
                    "Body temperature",
                    "8310-5",
                    "Body temperature measured at the axilla",
                    ClinicalVariableCategory.Clinical,
                    "Cel",
                    isRequired: true,
                    acceptedRange: NewRange(34.0m, 42.0m, "Cel"),
                    normalRange: NewRange(36.1m, 37.2m, "Cel")),
            ], TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _handler.HandleAsync(
                new GetDiagnoseFormQuery(), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            var age = Assert.Single(result.Data.ClinicalVariables, x => x.Code == "30525-0");
            Assert.NotNull(age.AcceptedRange);
            Assert.Equal(0m, age.AcceptedRange.Min);
            Assert.Equal(120m, age.AcceptedRange.Max);
            Assert.True(age.AcceptedRange.IsMinExclusive);
            Assert.True(age.AcceptedRange.IsMaxExclusive);
            Assert.Null(age.NormalRange);

            var glucose = Assert.Single(result.Data.ClinicalVariables, x => x.Code == "2339-0");
            Assert.NotNull(glucose.AcceptedRange);
            Assert.Equal(20m, glucose.AcceptedRange.Min);
            Assert.Equal(800m, glucose.AcceptedRange.Max);
            Assert.False(glucose.AcceptedRange.IsMinExclusive);
            Assert.False(glucose.AcceptedRange.IsMaxExclusive);
            Assert.NotNull(glucose.NormalRange);
            Assert.Equal(70m, glucose.NormalRange.Min);
            Assert.Equal(99m, glucose.NormalRange.Max);

            var temperature = Assert.Single(result.Data.ClinicalVariables, x => x.Code == "8310-5");
            Assert.NotNull(temperature.AcceptedRange);
            Assert.Equal(34.0m, temperature.AcceptedRange.Min);
            Assert.Equal(42.0m, temperature.AcceptedRange.Max);
            Assert.NotNull(temperature.NormalRange);
            Assert.Equal(36.1m, temperature.NormalRange.Min);
            Assert.Equal(37.2m, temperature.NormalRange.Max);
            Assert.Equal("Cel", temperature.NormalRange.Unit);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task GetDiagnoseForm_NoClinicalVariables_Fails()
        {
            // Antibiotics and pathogens alone cannot produce a usable form, so the
            // handler must stop with a business rule violation
            var group = Group("Beta-lactams");
            await _context.AntibioticGroups.AddAsync(
                group, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            await _context.Antibiotics.AddAsync(
                Antibiotic("Amoxicillin", group.Id, AwareClassification.Access, Id(1)),
                TestContext.Current.CancellationToken);
            await _context.Pathogens.AddAsync(
                Pathogen("Streptococcus pneumoniae", isAtypical: false, Id(2)),
                TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _handler.HandleAsync(
                new GetDiagnoseFormQuery(), TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.Null(result.Data);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.Error.StatusCode);
        }

        [Fact]
        public async Task GetDiagnoseForm_OnlySoftDeletedClinicalVariables_Fails()
        {
            /*
             * Boundary: the table is not empty, but the live clinical variable count is
             * zero because every row is soft deleted. The form has nothing to render, so
             * it must fail exactly like an empty table.
             */
            await _context.ClinicalVariables.AddAsync(
                Boolean(
                    "Female sex",
                    "46098-0",
                    "Whether the patient is female",
                    ClinicalVariableCategory.PersonalInformation,
                    isRequired: true,
                    softDeleted: true),
                TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _handler.HandleAsync(
                new GetDiagnoseFormQuery(), TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.Null(result.Data);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.StatusCode);
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.BusinessRuleViolation, result.Error.StatusCode);
        }

        #endregion
    }
}

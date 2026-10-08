using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.ClinicalVariables.GetClinicalVariableById;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Domain.Models;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Application.Test.Features.ClinicalVariables.GetClinicalVariableById
{
    public class GetClinicalVariableByIdHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly GetClinicalVariableByIdHandler _handler;
        private readonly IDbContext _context;

        public GetClinicalVariableByIdHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>()
                .UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);

            // Initialize handler
            _handler = new(_context);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        public async ValueTask InitializeAsync()
        {
            // Clear leftover data so the SingleAsync / not found assertions are
            // deterministic across runs. IgnoreQueryFilters because soft deleted rows
            // are hidden by the query filter but still occupy the table
            await _context.ClinicalVariables.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        private async Task<ClinicalVariable> SeedAsync(ClinicalVariable variable)
        {
            await _context.ClinicalVariables.AddAsync(variable, TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return variable;
        }

        private static NumericClinicalVariable CreateSystolicBloodPressure()
        {
            return new NumericClinicalVariable
            {
                // LOINC 8480-6 - systolic blood pressure
                Code = "8480-6",
                Name = "Systolic blood pressure",
                Description = "Systolic blood pressure measured at the arm",
                CanonicalUnit = "mmHg",
                IsRequired = true,
                Category = ClinicalVariableCategory.Clinical,
                // Boundary: both accepted bounds are inclusive
                AcceptedRange = new Range
                {
                    Min = 0,
                    IsMinExclusive = false,
                    Max = 300,
                    IsMaxExclusive = false,
                    Unit = "mmHg",
                },
                NormalRange = new Range
                {
                    Min = 90,
                    IsMinExclusive = false,
                    Max = 140,
                    IsMaxExclusive = false,
                    Unit = "mmHg",
                },
            };
        }

        private static BooleanClinicalVariable CreateBooleanVariable(
            string code,
            string name,
            string description,
            Formula? prerequisite = null)
        {
            return new BooleanClinicalVariable
            {
                Code = code,
                Name = name,
                Description = description,
                IsRequired = false,
                Category = ClinicalVariableCategory.PersonalInformation,
                Prerequisite = prerequisite,
            };
        }

        #region Happy path

        [Fact]
        public async Task GetClinicalVariableById_NumericVariable_Success()
        {
            var seeded = await SeedAsync(CreateSystolicBloodPressure());

            var result = await _handler.HandleAsync(
                new GetClinicalVariableByIdQuery { Id = seeded.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(seeded.Id, result.Data.Id);
            Assert.Equal("8480-6", result.Data.Code);
            Assert.Equal("Systolic blood pressure", result.Data.Name);
            Assert.Equal("Systolic blood pressure measured at the arm", result.Data.Description);
            Assert.Equal(ClinicalValueType.Numeric, result.Data.ValueType);
            Assert.Equal(ClinicalVariableCategory.Clinical, result.Data.Category);
            Assert.True(result.Data.IsRequired);
            Assert.Equal("mmHg", result.Data.CanonicalUnit);
            Assert.Null(result.Data.Prerequisite);

            // Numeric branch: both ranges are projected, the categorical payload is not
            Assert.NotNull(result.Data.AcceptedRange);
            Assert.Equal(0m, result.Data.AcceptedRange.Min);
            Assert.False(result.Data.AcceptedRange.IsMinExclusive);
            Assert.Equal(300m, result.Data.AcceptedRange.Max);
            Assert.False(result.Data.AcceptedRange.IsMaxExclusive);
            Assert.Equal("mmHg", result.Data.AcceptedRange.Unit);

            Assert.NotNull(result.Data.NormalRange);
            Assert.Equal(90m, result.Data.NormalRange.Min);
            Assert.Equal(140m, result.Data.NormalRange.Max);
            Assert.Equal("mmHg", result.Data.NormalRange.Unit);
            Assert.Null(result.Data.AcceptedValues);
        }

        [Fact]
        public async Task GetClinicalVariableById_NumericVariableWithoutNormalRange_Success()
        {
            // Boundary: the normal range is optional for a numeric variable
            var glucose = await SeedAsync(new NumericClinicalVariable
            {
                // LOINC 2339-0 - blood glucose
                Code = "2339-0",
                Name = "Blood glucose",
                Description = "Blood glucose mass per volume in serum or plasma",
                CanonicalUnit = "mg/dL",
                IsRequired = true,
                Category = ClinicalVariableCategory.Paraclinical,
                AcceptedRange = new Range
                {
                    Min = 0,
                    IsMinExclusive = false,
                    Max = 1000,
                    IsMaxExclusive = false,
                    Unit = "mg/dL",
                },
                NormalRange = null,
            });

            var result = await _handler.HandleAsync(
                new GetClinicalVariableByIdQuery { Id = glucose.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(ClinicalValueType.Numeric, result.Data.ValueType);
            Assert.NotNull(result.Data.AcceptedRange);
            Assert.Equal(0m, result.Data.AcceptedRange.Min);
            Assert.Equal(1000m, result.Data.AcceptedRange.Max);
            Assert.Equal("mg/dL", result.Data.AcceptedRange.Unit);
            Assert.Null(result.Data.NormalRange);
        }

        [Fact]
        public async Task GetClinicalVariableById_NumericVariableUnboundedAcceptedRange_Success()
        {
            // Boundary: decimal.MaxValue is the convention for "no upper limit"
            var crp = await SeedAsync(new NumericClinicalVariable
            {
                // LOINC 1988-5 - C reactive protein
                Code = "1988-5",
                Name = "C reactive protein",
                Description = "C reactive protein mass per volume in serum or plasma",
                CanonicalUnit = "mg/L",
                IsRequired = false,
                Category = ClinicalVariableCategory.Paraclinical,
                AcceptedRange = new Range
                {
                    Min = 0,
                    IsMinExclusive = false,
                    Max = decimal.MaxValue,
                    IsMaxExclusive = false,
                    Unit = "mg/L",
                },
                NormalRange = new Range
                {
                    Min = 0,
                    IsMinExclusive = false,
                    Max = 10,
                    IsMaxExclusive = false,
                    Unit = "mg/L",
                },
            });

            var result = await _handler.HandleAsync(
                new GetClinicalVariableByIdQuery { Id = crp.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(0m, result.Data.AcceptedRange!.Min);
            Assert.Equal(decimal.MaxValue, result.Data.AcceptedRange.Max);
            Assert.Equal("mg/L", result.Data.AcceptedRange.Unit);
            Assert.Equal(10m, result.Data.NormalRange!.Max);
            Assert.False(result.Data.IsRequired);
            Assert.Equal(ClinicalVariableCategory.Paraclinical, result.Data.Category);
        }

        [Fact]
        public async Task GetClinicalVariableById_CategoricalVariable_Success()
        {
            var sarsCov2 = await SeedAsync(new CategoricalClinicalVariable(["DETECTED", "NOT_DETECTED"])
            {
                // LOINC 94500-6 - SARS-CoV-2 RNA detected
                Code = "94500-6",
                Name = "SARS-CoV-2 RNA",
                Description = "SARS-CoV-2 RNA detected in respiratory specimen",
                IsRequired = true,
                Category = ClinicalVariableCategory.Paraclinical,
            });

            var result = await _handler.HandleAsync(
                new GetClinicalVariableByIdQuery { Id = sarsCov2.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(ClinicalValueType.Categorical, result.Data.ValueType);
            Assert.Equal(ClinicalVariableCategory.Paraclinical, result.Data.Category);
            Assert.True(result.Data.IsRequired);

            // Categorical branch: the accepted values are projected, the ranges are not
            Assert.NotNull(result.Data.AcceptedValues);
            Assert.Equal(["DETECTED", "NOT_DETECTED"], result.Data.AcceptedValues);
            Assert.Null(result.Data.AcceptedRange);
            Assert.Null(result.Data.NormalRange);
            Assert.Null(result.Data.CanonicalUnit);
        }

        [Fact]
        public async Task GetClinicalVariableById_BooleanVariableWithoutPrerequisite_Success()
        {
            var female = await SeedAsync(CreateBooleanVariable(
                "FEMALE",
                "Female sex",
                "Whether the patient is female"));

            var result = await _handler.HandleAsync(
                new GetClinicalVariableByIdQuery { Id = female.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(ClinicalValueType.Boolean, result.Data.ValueType);
            Assert.Equal(ClinicalVariableCategory.PersonalInformation, result.Data.Category);

            // Boolean branch: neither of the type specific payloads is projected
            Assert.Null(result.Data.Prerequisite);
            Assert.Null(result.Data.AcceptedRange);
            Assert.Null(result.Data.NormalRange);
            Assert.Null(result.Data.AcceptedValues);
        }

        [Fact]
        public async Task GetClinicalVariableById_VariablePrerequisite_RenderedAsVariableCode_Success()
        {
            var female = await SeedAsync(CreateBooleanVariable(
                "FEMALE",
                "Female sex",
                "Whether the patient is female"));
            var pregnant = await SeedAsync(CreateBooleanVariable(
                "PREGNANT",
                "Pregnant",
                "Whether the patient is currently pregnant",
                new VariableFormula(female)));

            var result = await _handler.HandleAsync(
                new GetClinicalVariableByIdQuery { Id = pregnant.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            // Business rule: the prerequisite is exposed as its string representation
            Assert.Equal(pregnant.Prerequisite!.ToString(), result.Data.Prerequisite);
        }

        [Fact]
        public async Task GetClinicalVariableById_BinaryPrerequisite_RenderedAsExpression_Success()
        {
            var sbp = await SeedAsync(CreateSystolicBloodPressure());
            var hypertension = await SeedAsync(CreateBooleanVariable(
                "HYPERTENSION-STAGE-2",
                "Hypertension stage 2",
                "Whether the systolic blood pressure is at stage 2 hypertension level",
                new BinaryFormula(
                    new VariableFormula(sbp),
                    new NumericConstantFormula(140),
                    ExpressionOperator.GT)));

            var result = await _handler.HandleAsync(
                new GetClinicalVariableByIdQuery { Id = hypertension.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            Assert.Equal(hypertension.Prerequisite!.ToString(), result.Data.Prerequisite);
        }

        [Fact]
        public async Task GetClinicalVariableById_NestedPrerequisite_RenderedWithParentheses_Success()
        {
            var female = await SeedAsync(CreateBooleanVariable(
                "FEMALE",
                "Female sex",
                "Whether the patient is female"));
            var sbp = await SeedAsync(CreateSystolicBloodPressure());
            var pih = await SeedAsync(CreateBooleanVariable(
                "PREGNANCY-INDUCED-HYPERTENSION",
                "Pregnancy induced hypertension",
                "Whether the patient has pregnancy induced hypertension",
                new BinaryFormula(
                    new VariableFormula(female),
                    new BinaryFormula(
                        new VariableFormula(sbp),
                        new NumericConstantFormula(140),
                        ExpressionOperator.GT),
                    ExpressionOperator.AND)));

            var result = await _handler.HandleAsync(
                new GetClinicalVariableByIdQuery { Id = pih.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            // Business rule: only non-leaf operands are parenthesized so the rendering
            // of the recursive formula tree stays unambiguous
            Assert.Equal(pih.Prerequisite!.ToString(), result.Data.Prerequisite);
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task GetClinicalVariableById_UnknownId_Fail()
        {
            var unknownId = Guid.CreateVersion7();

            var result = await _handler.HandleAsync(
                new GetClinicalVariableByIdQuery { Id = unknownId },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetClinicalVariableById_EmptyId_Fail()
        {
            // Boundary: Guid.Empty can never match a stored variable
            var result = await _handler.HandleAsync(
                new GetClinicalVariableByIdQuery { Id = Guid.Empty },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task GetClinicalVariableById_SoftDeletedVariable_Fail()
        {
            // The !IsDeleted query filter hides soft deleted rows, so reading one
            // is reported exactly like an unknown id
            var softDeleted = await SeedAsync(CreateBooleanVariable(
                "FEMALE",
                "Female sex",
                "Whether the patient is female"));
            softDeleted.IsDeleted = true;
            softDeleted.DeletedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _handler.HandleAsync(
                new GetClinicalVariableByIdQuery { Id = softDeleted.Id },
                TestContext.Current.CancellationToken);

            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
            Assert.Equal(ApplicationStatus.ResourceNotFound, result.StatusCode);
            Assert.Null(result.Data);
        }

        #endregion
    }
}

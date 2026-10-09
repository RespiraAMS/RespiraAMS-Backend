using Microsoft.EntityFrameworkCore;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.ClinicalVariables.GetClinicalVariables;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;
using Range = Respira.Clinical.Domain.Models.Range;

namespace Respira.Application.Test.Features.ClinicalVariables.GetClinicalVariables
{
    public class GetClinicalVariablesHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly GetClinicalVariablesHandler _handler;
        private readonly IDbContext _context;

        public GetClinicalVariablesHandlerTest(PostgresFixture fixture)
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
            // Clear leftover data so the ordering / empty assertions are deterministic
            // across runs. IgnoreQueryFilters because soft deleted rows are hidden by the
            // query filter but still occupy the table
            await _context.ClinicalVariables.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        /*
         * Seeds three live variables plus one soft deleted one. The names are inserted
         * out of alphabetical order and start with B / C / S so the expected ascending
         * order holds under any PostgreSQL collation
         */
        private async Task SeedAsync()
        {
            await _context.ClinicalVariables.AddRangeAsync(
            [
                CreateNumeric(
                    "Systolic blood pressure",
                    "8480-6",
                    "Systolic blood pressure measured at the arm",
                    ClinicalVariableCategory.Clinical,
                    "mmHg",
                    300),
                CreateBoolean(
                    "Female sex",
                    "46098-0",
                    "Whether the patient is female",
                    ClinicalVariableCategory.PersonalInformation),
                CreateNumeric(
                    "Blood glucose",
                    "2339-0",
                    "Blood glucose mass per volume in serum or plasma",
                    ClinicalVariableCategory.Paraclinical,
                    "mg/dL",
                    1000),
                new CategoricalClinicalVariable(["DETECTED", "NOT_DETECTED"])
                {
                    // LOINC 94500-6 - SARS-CoV-2 RNA detected
                    Code = "94500-6",
                    Name = "SARS-CoV-2 RNA",
                    Description = "SARS-CoV-2 RNA detected in respiratory specimen",
                    IsRequired = true,
                    Category = ClinicalVariableCategory.Paraclinical,
                    IsDeleted = true,
                    DeletedAt = DateTimeOffset.UtcNow,
                },
            ], TestContext.Current.CancellationToken);
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        #region Happy path

        [Fact]
        public async Task GetClinicalVariables_ReturnsAllSortedByNameAscending_Success()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(
                new GetClinicalVariablesQuery(), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Equal(
            [
                "Blood glucose",
                "Female sex",
                "Systolic blood pressure",
            ], [.. result.Data.ClinicalVariables.Select(x => x.Name)]);

            // The projection must carry the identity together with the name
            var bloodGlucose = Assert.Single(
                result.Data.ClinicalVariables, x => x.Name == "Blood glucose");
            Assert.Equal("2339-0", bloodGlucose.Code);
            Assert.All(result.Data.ClinicalVariables, x => Assert.NotEqual(Guid.Empty, x.Id));
        }

        [Fact]
        public async Task GetClinicalVariables_ExcludesSoftDeleted_Success()
        {
            await SeedAsync();

            var result = await _handler.HandleAsync(
                new GetClinicalVariablesQuery(), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            // The soft deleted SARS-CoV-2 RNA variable is hidden by the query filter
            Assert.Equal(3, result.Data.ClinicalVariables.Count());
            Assert.DoesNotContain(result.Data.ClinicalVariables, x => x.Name == "SARS-CoV-2 RNA");
        }

        [Fact]
        public async Task GetClinicalVariables_AllValueTypesReturned_Success()
        {
            // The list endpoint is type agnostic: every TPH subtype comes back
            await SeedAsync();

            var result = await _handler.HandleAsync(
                new GetClinicalVariablesQuery(), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);

            var byCode = result.Data.ClinicalVariables.ToDictionary(x => x.Code, x => x.Name);
            Assert.Equal("Blood glucose", byCode["2339-0"]);
            Assert.Equal("Female sex", byCode["46098-0"]);
            Assert.Equal("Systolic blood pressure", byCode["8480-6"]);
        }

        /*=== boundary: no data at all ===*/

        [Fact]
        public async Task GetClinicalVariables_EmptyDatabase_ReturnsEmpty()
        {
            var result = await _handler.HandleAsync(
                new GetClinicalVariablesQuery(), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.NotNull(result.Data);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.Empty(result.Data.ClinicalVariables);
        }

        #endregion

        private static NumericClinicalVariable CreateNumeric(
            string name,
            string code,
            string description,
            ClinicalVariableCategory category,
            string unit,
            decimal acceptedMax)
        {
            return new NumericClinicalVariable
            {
                Code = code,
                Name = name,
                Description = description,
                CanonicalUnit = unit,
                IsRequired = true,
                Category = category,
                AcceptedRange = new Range
                {
                    Min = 0,
                    IsMinExclusive = false,
                    Max = acceptedMax,
                    IsMaxExclusive = false,
                    Unit = unit,
                },
            };
        }

        private static BooleanClinicalVariable CreateBoolean(
            string name,
            string code,
            string description,
            ClinicalVariableCategory category)
        {
            return new BooleanClinicalVariable
            {
                Code = code,
                Name = name,
                Description = description,
                IsRequired = false,
                Category = category,
            };
        }
    }
}

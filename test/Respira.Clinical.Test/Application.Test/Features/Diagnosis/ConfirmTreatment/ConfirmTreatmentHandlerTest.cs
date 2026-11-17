using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Clinical.Application.Contracts.Data;
using Respira.Clinical.Application.Features.Diagnosis.ConfirmTreatment;
using Respira.Clinical.Domain.Entities;
using Respira.Clinical.Domain.Enums;
using Respira.Clinical.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Clinical.Application.Test.Features.Diagnosis.ConfirmTreatment
{
    public class ConfirmTreatmentHandlerTest : IClassFixture<PostgresFixture>, IAsyncLifetime
    {
        private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

        private readonly DbContextOptions<ClinicalDbContext> _options;
        private readonly ConfirmTreatmentHandler _handler;
        private readonly IDbContext _context;
        private readonly Mock<IMessageBus> _bus;

        public ConfirmTreatmentHandlerTest(PostgresFixture fixture)
        {
            // Create handler dependencies
            _options = new DbContextOptionsBuilder<ClinicalDbContext>()
                .UseNpgsql(fixture.ConnectionString).Options;
            _context = new ClinicalDbContext(_options);
            // Strict so an unexpected message on the bus (wrong id, wrong order) fails
            _bus = new Mock<IMessageBus>(MockBehavior.Strict);
            var logger = new Mock<ILogger<ConfirmTreatmentHandler>>().Object;

            // Initialize handler
            _handler = new(_context, _bus.Object, logger);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        public async ValueTask InitializeAsync()
        {
            // Clear leftover data (children first for FK constraints). IgnoreQueryFilters
            // is needed because soft deleted rows are hidden by the query filter but
            // still occupy the table
            await _context.Antibiotics.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            // A group can reference its parent, so delete sub-groups before parents
            await _context.AntibioticGroups.IgnoreQueryFilters()
                .Where(x => x.ParentId != null)
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await _context.AntibioticGroups.IgnoreQueryFilters()
                .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        #region Seed helpers

        private sealed record SeedData(
            AntibioticGroup BetaLactams,
            AntibioticGroup Cephalosporins,
            AntibioticGroup Carbapenems,
            AntibioticGroup Macrolides,
            AntibioticGroup Fluoroquinolones,
            AntibioticGroup Glycopeptides,
            Antibiotic Amoxicillin,
            Antibiotic Ampicillin,
            Antibiotic Ceftriaxone,
            Antibiotic Meropenem,
            Antibiotic Azithromycin,
            Antibiotic Levofloxacin,
            Antibiotic Vancomycin,
            Antibiotic RetiredAntibiotic);

        private static AntibioticGroup Group(string name, Guid? parentId = null)
        {
            return new AntibioticGroup
            {
                Name = name,
                Description = $"{name} antibiotic group",
                ParentId = parentId,
            };
        }

        private static Antibiotic Antibiotic(
            string name,
            Guid groupId,
            AwareClassification classification,
            bool softDeleted = false)
        {
            return new Antibiotic
            {
                Name = name,
                AntibioticGroupId = groupId,
                Classification = classification,
                IsDeleted = softDeleted,
                DeletedAt = softDeleted ? DateTimeOffset.UtcNow : null,
            };
        }

        /*
         * Seeds a realistic antibiotic catalogue with a two-level group hierarchy
         * (beta-lactams branch into cephalosporins and carbapenems), so both the
         * "same group" and the "common ancestor" business rules can be exercised.
         */
        private async Task<SeedData> SeedAsync()
        {
            var betaLactams = Group("Beta-lactams");
            var cephalosporins = Group("Cephalosporins", betaLactams.Id);
            var carbapenems = Group("Carbapenems", betaLactams.Id);
            var macrolides = Group("Macrolides");
            var fluoroquinolones = Group("Fluoroquinolones");
            var glycopeptides = Group("Glycopeptides");

            var amoxicillin = Antibiotic("Amoxicillin", betaLactams.Id, AwareClassification.Access);
            var ampicillin = Antibiotic("Ampicillin", betaLactams.Id, AwareClassification.Access);
            var ceftriaxone = Antibiotic("Ceftriaxone", cephalosporins.Id, AwareClassification.Watch);
            var meropenem = Antibiotic("Meropenem", carbapenems.Id, AwareClassification.Reserve);
            var azithromycin = Antibiotic("Azithromycin", macrolides.Id, AwareClassification.Watch);
            var levofloxacin = Antibiotic("Levofloxacin", fluoroquinolones.Id, AwareClassification.Watch);
            var vancomycin = Antibiotic("Vancomycin", glycopeptides.Id, AwareClassification.Watch);
            var telavancin = Antibiotic(
                "Telavancin", glycopeptides.Id, AwareClassification.Watch, softDeleted: true);

            await using var seedContext = new ClinicalDbContext(_options);
            seedContext.AntibioticGroups.AddRange(
                betaLactams,
                cephalosporins,
                carbapenems,
                macrolides,
                fluoroquinolones,
                glycopeptides);
            seedContext.Antibiotics.AddRange(
                amoxicillin,
                ampicillin,
                ceftriaxone,
                meropenem,
                azithromycin,
                levofloxacin,
                vancomycin,
                telavancin);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            return new SeedData(
                betaLactams,
                cephalosporins,
                carbapenems,
                macrolides,
                fluoroquinolones,
                glycopeptides,
                amoxicillin,
                ampicillin,
                ceftriaxone,
                meropenem,
                azithromycin,
                levofloxacin,
                vancomycin,
                telavancin);
        }

        #endregion

        #region Query and diagnosis helpers

        private static ConfirmTreatmentQuery CreateQuery(
            JsonElement diagnosis,
            List<Guid> medicines,
            string? reason = null,
            Guid? doctorId = null,
            Guid? patientId = null)
        {
            return new ConfirmTreatmentQuery
            {
                DoctorId = doctorId ?? Guid.CreateVersion7(),
                PatientId = patientId ?? Guid.CreateVersion7(),
                Diagnosis = diagnosis,
                Medicines = medicines,
                ReasonForMedicineChoice = reason,
            };
        }

        /*
         * Builds the raw diagnosis snapshot the doctor would echo back. Only the
         * fields read by the handler are relevant, but the rest of the DiagnoseResult
         * shape is included so the payload stays realistic.
         */
        private static JsonElement Diagnosis(Guid diagnosisId, params Guid[][] combinations)
        {
            var payload = new
            {
                diagnosisId,
                severity = "Severe",
                treatmentSite = "Inpatient",
                egfr = 48.5,
                isPatientPregnantOrInLactationPhase = false,
                protocols = new[]
                {
                    new
                    {
                        id = Guid.CreateVersion7(),
                        severity = "Severe",
                        treatmentSite = "Inpatient",
                        medicines = combinations
                            .Select(combination => combination
                                .Select(id => new
                                {
                                    id,
                                    name = "Antibiotic",
                                    classification = "Watch",
                                })
                                .ToArray())
                            .ToArray(),
                    },
                },
            };

            return JsonSerializer.SerializeToElement(payload, WebOptions);
        }

        private static JsonElement DiagnosisWithoutProtocols(Guid diagnosisId)
        {
            return JsonSerializer.SerializeToElement(new { diagnosisId }, WebOptions);
        }

        private static JsonElement DiagnosisWithNullProtocols(Guid diagnosisId)
        {
            return JsonSerializer.SerializeToElement(
                new { diagnosisId, protocols = (object?)null }, WebOptions);
        }

        private static JsonElement DiagnosisWithNullMedicines(Guid diagnosisId)
        {
            return JsonSerializer.SerializeToElement(
                new
                {
                    diagnosisId,
                    protocols = new[] { new { medicines = (object?)null } },
                },
                WebOptions);
        }

        private static JsonElement DiagnosisWithEmptyCombination(Guid diagnosisId)
        {
            return Parse(
                $$"""
                {
                  "diagnosisId": "{{diagnosisId}}",
                  "protocols": [{ "medicines": [[]] }]
                }
                """);
        }

        private static JsonElement Parse(string json)
        {
            // Clone so the element stays valid after the document is disposed
            return JsonDocument.Parse(json).RootElement.Clone();
        }

        #endregion

        #region Bus helpers

        private void SetupBusSuccess(Action<UpsertTreatmentPlanCommand>? capture = null)
        {
            _bus
                .Setup(x => x.InvokeAsync<Result<UpsertTreatmentPlanResult>>(
                    It.IsAny<object>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()))
                .Returns<object, CancellationToken, TimeSpan?>((message, _, _) =>
                {
                    capture?.Invoke(Assert.IsType<UpsertTreatmentPlanCommand>(message));
                    return Task.FromResult(
                        Result<UpsertTreatmentPlanResult>.Success(
                            ApplicationStatus.Success,
                            new UpsertTreatmentPlanResult(Guid.CreateVersion7())));
                });
        }

        private void SetupBusFailure(ApplicationError error)
        {
            _bus
                .Setup(x => x.InvokeAsync<Result<UpsertTreatmentPlanResult>>(
                    It.IsAny<object>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()))
                .Returns<object, CancellationToken, TimeSpan?>((_, _, _) =>
                    Task.FromResult(
                        Result<UpsertTreatmentPlanResult>.Failure(error)));
        }

        private void VerifyBusInvokedOnce()
        {
            _bus.Verify(x => x.InvokeAsync<Result<UpsertTreatmentPlanResult>>(
                It.IsAny<object>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()), Times.Once);
        }

        private void VerifyBusNotInvoked()
        {
            _bus.Verify(x => x.InvokeAsync<Result<UpsertTreatmentPlanResult>>(
                It.IsAny<object>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<TimeSpan?>()), Times.Never);
        }

        private static void AssertFailure(Result result, string expectedStatusCode)
        {
            Assert.True(result.IsFailure());
            Assert.Equal(expectedStatusCode, result.StatusCode);
            Assert.NotNull(result.Error);
            Assert.Equal(expectedStatusCode, result.Error.StatusCode);
        }

        #endregion

        #region Happy path

        [Fact]
        public async Task ConfirmTreatment_AdherentCombination_ForwardsTreatmentPlan_Success()
        {
            var seed = await SeedAsync();
            var diagnosisId = Guid.CreateVersion7();
            var doctorId = Guid.CreateVersion7();
            var patientId = Guid.CreateVersion7();

            // Conventional moderate CAP therapy: 3rd generation cephalosporin + macrolide
            var diagnosis = Diagnosis(
                diagnosisId, [seed.Ceftriaxone.Id, seed.Azithromycin.Id]);

            UpsertTreatmentPlanCommand? captured = null;
            SetupBusSuccess(command => captured = command);

            var query = CreateQuery(
                diagnosis,
                [seed.Ceftriaxone.Id, seed.Azithromycin.Id],
                doctorId: doctorId,
                patientId: patientId);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            Assert.NotNull(captured);
            Assert.Equal(doctorId, captured.DoctorId);
            Assert.Equal(patientId, captured.PatientId);
            Assert.Equal(diagnosisId, captured.DiagnosisId);
            // The pick matches a protocol combination exactly, so no reason is needed
            Assert.True(captured.IsAdhere);
            Assert.Null(captured.ReasonForMedicineChoice);

            // The client snapshot is forwarded byte for byte, never re-serialized
            Assert.Equal(query.Diagnosis.GetRawText(), captured.Diagnosis);

            // The medicine catalogue data is resolved from the database
            Assert.Equal(2, captured.Medicines.Count);
            var ceftriaxone = Assert.Single(
                captured.Medicines, x => x.Id == seed.Ceftriaxone.Id);
            Assert.Equal("Ceftriaxone", ceftriaxone.Name);
            Assert.Equal(AwareClassification.Watch, ceftriaxone.Classification);
            var azithromycin = Assert.Single(
                captured.Medicines, x => x.Id == seed.Azithromycin.Id);
            Assert.Equal("Azithromycin", azithromycin.Name);
            Assert.Equal(AwareClassification.Watch, azithromycin.Classification);

            VerifyBusInvokedOnce();
        }

        [Fact]
        public async Task ConfirmTreatment_SingleMedicineCombination_Success()
        {
            // Boundary: 1 medicine is the smallest possible protocol combination
            // (a single antibiotic never shares a group with itself)
            var seed = await SeedAsync();
            var diagnosis = Diagnosis(Guid.CreateVersion7(), [seed.Levofloxacin.Id]);

            UpsertTreatmentPlanCommand? captured = null;
            SetupBusSuccess(command => captured = command);

            var query = CreateQuery(diagnosis, [seed.Levofloxacin.Id]);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(captured);
            Assert.True(captured.IsAdhere);
            Assert.Equal([seed.Levofloxacin.Id], captured.Medicines.Select(x => x.Id));

            VerifyBusInvokedOnce();
        }

        [Fact]
        public async Task ConfirmTreatment_AdherentCombinationInDifferentOrder_Success()
        {
            // Adherence is a set comparison: the order the doctor picked the medicines
            // does not matter
            var seed = await SeedAsync();
            var diagnosis = Diagnosis(
                Guid.CreateVersion7(), [seed.Ceftriaxone.Id, seed.Azithromycin.Id]);

            UpsertTreatmentPlanCommand? captured = null;
            SetupBusSuccess(command => captured = command);

            var query = CreateQuery(
                diagnosis, [seed.Azithromycin.Id, seed.Ceftriaxone.Id]);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(captured);
            Assert.True(captured.IsAdhere);
            Assert.Null(captured.ReasonForMedicineChoice);
            Assert.Equal(
                new[] { seed.Ceftriaxone.Id, seed.Azithromycin.Id }.OrderBy(x => x.ToString()),
                captured.Medicines.Select(x => x.Id).OrderBy(x => x.ToString()));

            VerifyBusInvokedOnce();
        }

        [Fact]
        public async Task ConfirmTreatment_AdherentCombinationInLaterProtocol_Success()
        {
            var seed = await SeedAsync();
            var diagnosis = Parse(
                $$"""
                {
                  "diagnosisId": "{{Guid.CreateVersion7()}}",
                  "protocols": [
                    { "medicines": [[{ "id": "{{seed.Levofloxacin.Id}}" }]] },
                    {
                      "medicines": [
                        [
                          { "id": "{{seed.Ceftriaxone.Id}}" },
                          { "id": "{{seed.Azithromycin.Id}}" }
                        ]
                      ]
                    }
                  ]
                }
                """);

            UpsertTreatmentPlanCommand? captured = null;
            SetupBusSuccess(command => captured = command);

            var query = CreateQuery(
                diagnosis, [seed.Ceftriaxone.Id, seed.Azithromycin.Id]);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(captured);
            Assert.True(captured.IsAdhere);

            VerifyBusInvokedOnce();
        }

        [Fact]
        public async Task ConfirmTreatment_AdherentCombinationWithReason_Success()
        {
            // A reason is optional when the pick adheres to a protocol
            var seed = await SeedAsync();
            var diagnosis = Diagnosis(
                Guid.CreateVersion7(), [seed.Ceftriaxone.Id, seed.Azithromycin.Id]);

            UpsertTreatmentPlanCommand? captured = null;
            SetupBusSuccess(command => captured = command);

            const string reason = "Renal dosing adjusted for eGFR 48.5 mL/min/1.73m2";
            var query = CreateQuery(
                diagnosis,
                [seed.Ceftriaxone.Id, seed.Azithromycin.Id],
                reason);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(captured);
            Assert.True(captured.IsAdhere);
            Assert.Equal(reason, captured.ReasonForMedicineChoice);

            VerifyBusInvokedOnce();
        }

        [Fact]
        public async Task ConfirmTreatment_NonAdherentWithReason_Success()
        {
            var seed = await SeedAsync();
            // Protocol recommends cephalosporin + macrolide, but the doctor picks
            // a carbapenem + glycopeptide (unrelated group lineages, so the
            // combination itself is valid)
            var diagnosis = Diagnosis(
                Guid.CreateVersion7(), [seed.Ceftriaxone.Id, seed.Azithromycin.Id]);

            UpsertTreatmentPlanCommand? captured = null;
            SetupBusSuccess(command => captured = command);

            const string reason =
                "Blood culture grew Pseudomonas aeruginosa; escalated to meropenem and added vancomycin for suspected MRSA";
            var query = CreateQuery(
                diagnosis, [seed.Meropenem.Id, seed.Vancomycin.Id], reason);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(captured);
            Assert.False(captured.IsAdhere);
            Assert.Equal(reason, captured.ReasonForMedicineChoice);
            Assert.Equal(
                new[] { seed.Meropenem.Id, seed.Vancomycin.Id }.OrderBy(x => x.ToString()),
                captured.Medicines.Select(x => x.Id).OrderBy(x => x.ToString()));

            VerifyBusInvokedOnce();
        }

        [Fact]
        public async Task ConfirmTreatment_DiagnosisWithoutProtocolsWithReason_Success()
        {
            // Boundary: 0 recommended protocols means any pick is non-adherent,
            // which is allowed as long as a reason is supplied
            var seed = await SeedAsync();
            var diagnosisId = Guid.CreateVersion7();
            var diagnosis = DiagnosisWithoutProtocols(diagnosisId);

            UpsertTreatmentPlanCommand? captured = null;
            SetupBusSuccess(command => captured = command);

            var query = CreateQuery(
                diagnosis,
                [seed.Levofloxacin.Id],
                "No protocol matched the patient's severity and treatment site");

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(captured);
            Assert.Equal(diagnosisId, captured.DiagnosisId);
            Assert.False(captured.IsAdhere);
            Assert.Equal(
                "No protocol matched the patient's severity and treatment site",
                captured.ReasonForMedicineChoice);

            VerifyBusInvokedOnce();
        }

        [Fact]
        public async Task ConfirmTreatment_NullProtocolsWithReason_Success()
        {
            // The client may send an explicit JSON null for protocols
            var seed = await SeedAsync();
            var diagnosis = DiagnosisWithNullProtocols(Guid.CreateVersion7());

            UpsertTreatmentPlanCommand? captured = null;
            SetupBusSuccess(command => captured = command);

            var query = CreateQuery(
                diagnosis,
                [seed.Levofloxacin.Id],
                "Protocol snapshot was trimmed by the client");

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(captured);
            Assert.False(captured.IsAdhere);

            VerifyBusInvokedOnce();
        }

        [Fact]
        public async Task ConfirmTreatment_ProtocolWithNullMedicinesWithReason_Success()
        {
            // A protocol whose medicine combinations are null must be skipped,
            // not crash the adherence check
            var seed = await SeedAsync();
            var diagnosis = DiagnosisWithNullMedicines(Guid.CreateVersion7());

            UpsertTreatmentPlanCommand? captured = null;
            SetupBusSuccess(command => captured = command);

            var query = CreateQuery(
                diagnosis,
                [seed.Levofloxacin.Id],
                "Protocol medicine list was not returned by the recommendation engine");

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(captured);
            Assert.False(captured.IsAdhere);

            VerifyBusInvokedOnce();
        }

        [Fact]
        public async Task ConfirmTreatment_ProtocolWithEmptyCombinationWithReason_Success()
        {
            // Boundary: an empty combination has 0 medicines, while the doctor
            // picked 1, so it cannot count as adherent
            var seed = await SeedAsync();
            var diagnosis = DiagnosisWithEmptyCombination(Guid.CreateVersion7());

            UpsertTreatmentPlanCommand? captured = null;
            SetupBusSuccess(command => captured = command);

            var query = CreateQuery(
                diagnosis,
                [seed.Levofloxacin.Id],
                "Recommended protocol had no medicine combination available");

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(captured);
            Assert.False(captured.IsAdhere);

            VerifyBusInvokedOnce();
        }

        [Fact]
        public async Task ConfirmTreatment_RawDiagnosis_ForwardedVerbatim_Success()
        {
            var seed = await SeedAsync();
            var diagnosisId = Guid.CreateVersion7();

            // Deliberately formatted with indentation, because the forwarded text
            // must be the exact snapshot the doctor saw, not a re-serialized copy
            var raw = $$"""
                {
                  "diagnosisId": "{{diagnosisId}}",
                  "severity": "Severe",
                  "treatmentSite": "Inpatient",
                  "egfr": 48.5,
                  "protocols": [
                    {
                      "medicines": [
                        [
                          { "id": "{{seed.Ceftriaxone.Id}}", "name": "Ceftriaxone", "classification": "Watch" },
                          { "id": "{{seed.Azithromycin.Id}}", "name": "Azithromycin", "classification": "Watch" }
                        ]
                      ]
                    }
                  ]
                }
                """;
            var diagnosis = Parse(raw);

            UpsertTreatmentPlanCommand? captured = null;
            SetupBusSuccess(command => captured = command);

            var query = CreateQuery(
                diagnosis, [seed.Ceftriaxone.Id, seed.Azithromycin.Id]);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess());
            Assert.NotNull(captured);
            Assert.Equal(raw, captured.Diagnosis);
            Assert.Equal(query.Diagnosis.GetRawText(), captured.Diagnosis);

            VerifyBusInvokedOnce();
        }

        #endregion

        #region Fail path

        [Fact]
        public async Task ConfirmTreatment_DefaultDiagnosis_Fails()
        {
            // Boundary: a missing JSON body is ValueKind.Undefined, not an object
            var query = CreateQuery(default, [Guid.CreateVersion7()]);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertFailure(result, ApplicationStatus.BadRequest);
            VerifyBusNotInvoked();
        }

        // Any JSON kind other than an object must be rejected before any DB work
        [Theory]
        [InlineData("[]")]
        [InlineData("[\"diagnosis\"]")]
        [InlineData("\"Severe\"")]
        [InlineData("42")]
        [InlineData("true")]
        [InlineData("null")]
        public async Task ConfirmTreatment_NonObjectDiagnosis_Fails(string diagnosisJson)
        {
            var query = CreateQuery(Parse(diagnosisJson), [Guid.CreateVersion7()]);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertFailure(result, ApplicationStatus.BadRequest);
            VerifyBusNotInvoked();
        }

        // A JSON object whose members cannot bind to ConfirmDiagnosisView
        [Theory]
        [InlineData("{\"diagnosisId\":\"not-a-guid\"}")]
        [InlineData("{\"diagnosisId\":null}")]
        [InlineData("{\"diagnosisId\":42}")]
        [InlineData("{\"protocols\":\"not-an-array\"}")]
        [InlineData("{\"protocols\":[{\"medicines\":\"levofloxacin\"}]}")]
        [InlineData("{\"protocols\":[{\"medicines\":[[\"levofloxacin\"]]}]}")]
        public async Task ConfirmTreatment_MalformedDiagnosis_Fails(string diagnosisJson)
        {
            var query = CreateQuery(Parse(diagnosisJson), [Guid.CreateVersion7()]);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertFailure(result, ApplicationStatus.BadRequest);
            VerifyBusNotInvoked();
        }

        // Boundary: an object is a valid shape, but a missing or empty diagnosis ID
        // cannot identify the diagnosis being confirmed
        [Theory]
        [InlineData("{}")]
        [InlineData("{\"diagnosisId\":\"00000000-0000-0000-0000-000000000000\"}")]
        [InlineData("{\"protocols\":[]}")]
        public async Task ConfirmTreatment_MissingDiagnosisId_Fails(string diagnosisJson)
        {
            var query = CreateQuery(Parse(diagnosisJson), [Guid.CreateVersion7()]);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertFailure(result, ApplicationStatus.BadRequest);
            VerifyBusNotInvoked();
        }

        [Fact]
        public async Task ConfirmTreatment_UnknownMedicine_Fails()
        {
            var seed = await SeedAsync();
            var diagnosis = Diagnosis(Guid.CreateVersion7(), [seed.Levofloxacin.Id]);
            // Boundary: 1 of the 2 provided IDs does not exist in the catalogue
            var query = CreateQuery(
                diagnosis, [seed.Levofloxacin.Id, Guid.CreateVersion7()]);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertFailure(result, ApplicationStatus.BadRequest);
            VerifyBusNotInvoked();
        }

        [Fact]
        public async Task ConfirmTreatment_RetiredMedicine_Fails()
        {
            var seed = await SeedAsync();
            var diagnosis = Diagnosis(Guid.CreateVersion7(), [seed.RetiredAntibiotic.Id]);
            // The medicine still exists in the table, but it is soft deleted and
            // therefore must not be prescribable anymore
            var query = CreateQuery(
                diagnosis, [seed.RetiredAntibiotic.Id], "Legacy protocol");

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertFailure(result, ApplicationStatus.BadRequest);
            VerifyBusNotInvoked();
        }

        [Fact]
        public async Task ConfirmTreatment_DuplicateMedicine_Fails()
        {
            var seed = await SeedAsync();
            var diagnosis = Diagnosis(Guid.CreateVersion7(), [seed.Levofloxacin.Id]);
            // Boundary: 2 provided entries resolve to 1 catalogue medicine
            var query = CreateQuery(
                diagnosis, [seed.Levofloxacin.Id, seed.Levofloxacin.Id], "Duplicate");

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertFailure(result, ApplicationStatus.BadRequest);
            VerifyBusNotInvoked();
        }

        [Fact]
        public async Task ConfirmTreatment_NonAdherentWithoutReason_Fails()
        {
            var seed = await SeedAsync();
            var diagnosis = Diagnosis(
                Guid.CreateVersion7(), [seed.Ceftriaxone.Id, seed.Azithromycin.Id]);
            // Different protocol pick with no justification
            var query = CreateQuery(
                diagnosis, [seed.Meropenem.Id, seed.Vancomycin.Id], reason: null);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertFailure(result, ApplicationStatus.BusinessRuleViolation);
            VerifyBusNotInvoked();
        }

        // Boundary: a reason consisting only of whitespace is treated as missing
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        [InlineData("\t")]
        [InlineData("\n")]
        public async Task ConfirmTreatment_NonAdherentWithBlankReason_Fails(string reason)
        {
            var seed = await SeedAsync();
            var diagnosis = Diagnosis(
                Guid.CreateVersion7(), [seed.Ceftriaxone.Id, seed.Azithromycin.Id]);
            var query = CreateQuery(
                diagnosis, [seed.Meropenem.Id, seed.Vancomycin.Id], reason);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertFailure(result, ApplicationStatus.BusinessRuleViolation);
            VerifyBusNotInvoked();
        }

        [Fact]
        public async Task ConfirmTreatment_SameCountButDifferentMedicineWithoutReason_Fails()
        {
            var seed = await SeedAsync();
            var diagnosis = Diagnosis(
                Guid.CreateVersion7(), [seed.Ceftriaxone.Id, seed.Azithromycin.Id]);
            // Boundary: same number of medicines as the protocol, but one ID differs,
            // so the pick is not adherent
            var query = CreateQuery(
                diagnosis, [seed.Ceftriaxone.Id, seed.Levofloxacin.Id], reason: null);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertFailure(result, ApplicationStatus.BusinessRuleViolation);
            VerifyBusNotInvoked();
        }

        [Fact]
        public async Task ConfirmTreatment_SubsetOfProtocolCombinationWithoutReason_Fails()
        {
            var seed = await SeedAsync();
            var diagnosis = Diagnosis(
                Guid.CreateVersion7(), [seed.Ceftriaxone.Id, seed.Azithromycin.Id]);
            // Boundary: one medicine fewer than the protocol combination
            var query = CreateQuery(
                diagnosis, [seed.Ceftriaxone.Id], reason: null);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertFailure(result, ApplicationStatus.BusinessRuleViolation);
            VerifyBusNotInvoked();
        }

        [Fact]
        public async Task ConfirmTreatment_SupersetOfProtocolCombinationWithoutReason_Fails()
        {
            var seed = await SeedAsync();
            var diagnosis = Diagnosis(
                Guid.CreateVersion7(), [seed.Ceftriaxone.Id, seed.Azithromycin.Id]);
            // Boundary: one medicine more than the protocol combination. All three
            // belong to different lineages, so only adherence is violated
            var query = CreateQuery(
                diagnosis,
                [seed.Ceftriaxone.Id, seed.Azithromycin.Id, seed.Levofloxacin.Id],
                reason: null);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertFailure(result, ApplicationStatus.BusinessRuleViolation);
            VerifyBusNotInvoked();
        }

        // The upstream error must be passed back with its original status code
        [Theory]
        [InlineData(ApplicationStatus.ResourceNotFound)]
        [InlineData(ApplicationStatus.BusinessRuleViolation)]
        [InlineData(ApplicationStatus.ServerError)]
        [InlineData(ApplicationStatus.Timeout)]
        public async Task ConfirmTreatment_TreatmentPlanCreationFails_PropagatesError(
            string upstreamStatusCode)
        {
            var seed = await SeedAsync();
            var diagnosis = Diagnosis(
                Guid.CreateVersion7(), [seed.Ceftriaxone.Id, seed.Azithromycin.Id]);

            var upstreamError = new ApplicationError(
                upstreamStatusCode,
                "The patient service could not persist the treatment plan");
            SetupBusFailure(upstreamError);

            var query = CreateQuery(
                diagnosis, [seed.Ceftriaxone.Id, seed.Azithromycin.Id]);

            var result = await _handler.HandleAsync(
                query, TestContext.Current.CancellationToken);

            AssertFailure(result, upstreamStatusCode);
            Assert.Contains(upstreamError.Description, result.Error!.Description);

            VerifyBusInvokedOnce();
        }

        #endregion
    }
}

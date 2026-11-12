using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Moq;
using Respira.Analysis.Application.Contracts;
using Respira.Analysis.Application.Features.Commands.Create;
using Respira.Analysis.Domain.Entities;
using Respira.Analysis.Infrastructure.Data;
using Respira.ServiceDefaults.Contracts.Messages.Patients;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Analysis.Application.Test.Features.Commands.Create;

public class CreateAnalysisReportCommandHandlerTest
    : IClassFixture<PostgresFixture>,
        IAsyncLifetime
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly DbContextOptions<AnalysisDbContext> _options;
    private readonly AnalysisDbContext _context;

    public CreateAnalysisReportCommandHandlerTest(PostgresFixture fixture)
    {
        _options = new DbContextOptionsBuilder<AnalysisDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        _context = new AnalysisDbContext(_options);
    }

    public async ValueTask InitializeAsync()
    {
        await using var context = new AnalysisDbContext(_options);
        await context
            .ClinicalAnalyses.IgnoreQueryFilters()
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task Create_StartAfterEnd_ReturnsBadRequest()
    {
        var handler = CreateHandler(CreateBus(new PatientsByDoctorResult()));

        var result = await handler.HandleAsync(
            new CreateAnalysisReportCommand
            {
                DoctorId = Guid.CreateVersion7(),
                Start = End,
                End = Start,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
        Assert.Equal(
            "Start must be before end, and optional doctor ID must be valid",
            result.Error?.Description
        );
    }

    [Fact]
    public async Task Create_StartEqualsEnd_ReturnsBadRequest()
    {
        var handler = CreateHandler(CreateBus(new PatientsByDoctorResult()));

        var result = await handler.HandleAsync(
            new CreateAnalysisReportCommand
            {
                DoctorId = Guid.CreateVersion7(),
                Start = Start,
                End = Start,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
        Assert.Equal(
            "Start must be before end, and optional doctor ID must be valid",
            result.Error?.Description
        );
    }

    [Fact]
    public async Task Create_EmptyDoctorId_ReturnsBadRequest()
    {
        var handler = CreateHandler(CreateBus(new PatientsByDoctorResult()));

        var result = await handler.HandleAsync(
            new CreateAnalysisReportCommand
            {
                DoctorId = Guid.Empty,
                Start = Start,
                End = End,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.BadRequest, result.StatusCode);
        Assert.Equal(
            "Start must be before end, and optional doctor ID must be valid",
            result.Error?.Description
        );
    }

    [Fact]
    public async Task Create_BusReturnsNull_ReturnsServerError()
    {
        var handler = CreateHandler(CreateBus(null));

        var result = await handler.HandleAsync(
            new CreateAnalysisReportCommand
            {
                DoctorId = Guid.CreateVersion7(),
                Start = Start,
                End = End,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ServerError, result.StatusCode);
        Assert.Equal(
            "Patient service did not return analysis source data",
            result.Error?.Description
        );
    }

    [Fact]
    public async Task Create_BusReturnsNullPatients_ReturnsServerError()
    {
        var handler = CreateHandler(CreateBus(new PatientsByDoctorResult { Patients = null! }));

        var result = await handler.HandleAsync(
            new CreateAnalysisReportCommand
            {
                DoctorId = Guid.CreateVersion7(),
                Start = Start,
                End = End,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ServerError, result.StatusCode);
        Assert.Equal(
            "Patient service did not return analysis source data",
            result.Error?.Description
        );
    }

    [Fact]
    public async Task Create_BusThrows_PropagatesException()
    {
        var bus = new Mock<IMessageBus>();
        bus.Setup(b =>
                b.InvokeAsync<PatientsByDoctorResult>(
                    It.IsAny<GetPatientsByDoctorQuery>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                )
            )
            .ThrowsAsync(new InvalidOperationException("patient service unavailable"));
        var handler = CreateHandler(bus);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(
                new CreateAnalysisReportCommand
                {
                    DoctorId = Guid.CreateVersion7(),
                    Start = Start,
                    End = End,
                },
                TestContext.Current.CancellationToken
            )
        );

        Assert.Equal("patient service unavailable", exception.Message);
    }

    [Fact]
    public async Task Create_NoPatients_PersistsEmptyReport()
    {
        var doctorId = Guid.CreateVersion7();
        var handler = CreateHandler(CreateBus(new PatientsByDoctorResult()));

        var result = await handler.HandleAsync(
            new CreateAnalysisReportCommand
            {
                DoctorId = doctorId,
                Start = Start,
                End = End,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Created, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Equal(0, result.Data.TotalPatients);
        Assert.Equal(0, result.Data.TotalTreatmentPlans);
        Assert.Equal(0, result.Data.TotalAdherentPlans);
        Assert.Equal(0, result.Data.TotalNonAdherentPlans);
        Assert.Equal(0, result.Data.TotalPendingReviewPlans);
        Assert.Equal(0, result.Data.TotalDeceasedPatients);
        Assert.Equal(0, result.Data.TotalRecoveredPatients);
        Assert.Equal(0, result.Data.TotalOtherOutcomePatients);
        Assert.Empty(result.Data.Doctors);

        var stored = await LoadAnalysisAsync(result.Data.AnalysisId);
        Assert.NotNull(stored);
        Assert.Equal("[]", stored.PatientIdsJson);
        Assert.Equal("[]", stored.TreatmentPlanIdsJson);
        Assert.False(string.IsNullOrWhiteSpace(stored.ReportJson));
    }

    [Fact]
    public async Task Create_SingleAdherentPlan_CountsCorrectly()
    {
        var doctorId = Guid.CreateVersion7();
        var patientId = Guid.CreateVersion7();
        var planId = Guid.CreateVersion7();
        var patient = CreatePatient(
            patientId,
            plans: [CreatePlan(planId, doctorId, PatientAnalysisConstants.AdherenceStates.Adhere)]
        );
        var handler = CreateHandler(CreateBus(new PatientsByDoctorResult { Patients = [patient] }));

        var result = await handler.HandleAsync(
            new CreateAnalysisReportCommand
            {
                DoctorId = doctorId,
                Start = Start,
                End = End,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalPatients);
        Assert.Equal(1, result.Data.TotalTreatmentPlans);
        Assert.Equal(1, result.Data.TotalAdherentPlans);
        Assert.Equal(0, result.Data.TotalNonAdherentPlans);
        Assert.Equal(0, result.Data.TotalPendingReviewPlans);
    }

    [Theory]
    [InlineData(PatientAnalysisConstants.AdherenceStates.NotAdhereValid)]
    [InlineData(PatientAnalysisConstants.AdherenceStates.NotAdhereInvalid)]
    public async Task Create_NonAdherentStates_CountAsNonAdherent(string adherenceState)
    {
        var doctorId = Guid.CreateVersion7();
        var patient = CreatePatient(
            Guid.CreateVersion7(),
            plans: [CreatePlan(Guid.CreateVersion7(), doctorId, adherenceState)]
        );
        var handler = CreateHandler(CreateBus(new PatientsByDoctorResult { Patients = [patient] }));

        var result = await handler.HandleAsync(
            new CreateAnalysisReportCommand
            {
                DoctorId = doctorId,
                Start = Start,
                End = End,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalTreatmentPlans);
        Assert.Equal(0, result.Data.TotalAdherentPlans);
        Assert.Equal(1, result.Data.TotalNonAdherentPlans);
        Assert.Equal(0, result.Data.TotalPendingReviewPlans);
    }

    [Fact]
    public async Task Create_NotAdhere_CountsAsPendingReview()
    {
        var doctorId = Guid.CreateVersion7();
        var patient = CreatePatient(
            Guid.CreateVersion7(),
            plans:
            [
                CreatePlan(
                    Guid.CreateVersion7(),
                    doctorId,
                    PatientAnalysisConstants.AdherenceStates.NotAdhere
                ),
            ]
        );
        var handler = CreateHandler(CreateBus(new PatientsByDoctorResult { Patients = [patient] }));

        var result = await handler.HandleAsync(
            new CreateAnalysisReportCommand
            {
                DoctorId = doctorId,
                Start = Start,
                End = End,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalTreatmentPlans);
        Assert.Equal(0, result.Data.TotalAdherentPlans);
        Assert.Equal(0, result.Data.TotalNonAdherentPlans);
        Assert.Equal(1, result.Data.TotalPendingReviewPlans);
    }

    [Fact]
    public async Task Create_DeathOutcome_CountsDeceasedPatients()
    {
        var doctorId = Guid.CreateVersion7();
        var patient = CreatePatient(
            Guid.CreateVersion7(),
            treatmentOutcome: PatientAnalysisConstants.TreatmentOutcomes.Death
        );
        var handler = CreateHandler(CreateBus(new PatientsByDoctorResult { Patients = [patient] }));

        var result = await handler.HandleAsync(
            new CreateAnalysisReportCommand
            {
                DoctorId = doctorId,
                Start = Start,
                End = End,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalDeceasedPatients);
        Assert.Equal(0, result.Data.TotalRecoveredPatients);
        Assert.Equal(0, result.Data.TotalOtherOutcomePatients);
    }

    [Fact]
    public async Task Create_RecoveredOutcome_CountsRecoveredPatients()
    {
        var doctorId = Guid.CreateVersion7();
        var patient = CreatePatient(
            Guid.CreateVersion7(),
            treatmentOutcome: PatientAnalysisConstants.TreatmentOutcomes.Recovered
        );
        var handler = CreateHandler(CreateBus(new PatientsByDoctorResult { Patients = [patient] }));

        var result = await handler.HandleAsync(
            new CreateAnalysisReportCommand
            {
                DoctorId = doctorId,
                Start = Start,
                End = End,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Equal(0, result.Data.TotalDeceasedPatients);
        Assert.Equal(1, result.Data.TotalRecoveredPatients);
        Assert.Equal(0, result.Data.TotalOtherOutcomePatients);
    }

    [Fact]
    public async Task Create_InTreatmentOutcome_CountsOtherOutcomePatients()
    {
        var doctorId = Guid.CreateVersion7();
        var patient = CreatePatient(
            Guid.CreateVersion7(),
            treatmentOutcome: PatientAnalysisConstants.TreatmentOutcomes.InTreatment
        );
        var handler = CreateHandler(CreateBus(new PatientsByDoctorResult { Patients = [patient] }));

        var result = await handler.HandleAsync(
            new CreateAnalysisReportCommand
            {
                DoctorId = doctorId,
                Start = Start,
                End = End,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Equal(0, result.Data.TotalDeceasedPatients);
        Assert.Equal(0, result.Data.TotalRecoveredPatients);
        Assert.Equal(1, result.Data.TotalOtherOutcomePatients);
    }

    [Fact]
    public async Task Create_DoctorsSummary_ContainsPerDoctorBreakdown()
    {
        var doctorId = Guid.CreateVersion7();
        var patient = CreatePatient(
            Guid.CreateVersion7(),
            plans:
            [
                CreatePlan(
                    Guid.CreateVersion7(),
                    doctorId,
                    PatientAnalysisConstants.AdherenceStates.Adhere
                ),
                CreatePlan(
                    Guid.CreateVersion7(),
                    doctorId,
                    PatientAnalysisConstants.AdherenceStates.NotAdhereValid
                ),
                CreatePlan(
                    Guid.CreateVersion7(),
                    doctorId,
                    PatientAnalysisConstants.AdherenceStates.NotAdhere
                ),
            ]
        );
        var handler = CreateHandler(CreateBus(new PatientsByDoctorResult { Patients = [patient] }));

        var result = await handler.HandleAsync(
            new CreateAnalysisReportCommand
            {
                DoctorId = doctorId,
                Start = Start,
                End = End,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        var doctor = Assert.Single(result.Data.Doctors);
        Assert.Equal(doctorId, doctor.DoctorId);
        Assert.Equal(1, doctor.TotalPatients);
        Assert.Equal(3, doctor.TotalTreatmentPlans);
        Assert.Equal(1, doctor.TotalAdherentPlans);
        Assert.Equal(1, doctor.TotalNonAdherentPlans);
        Assert.Equal(1, doctor.TotalPendingReviewPlans);
        Assert.NotNull(doctor.Medicines);
    }

    [Fact]
    public async Task Create_MedicinesSummary_ContainsPerMedicineBreakdown()
    {
        var doctorId = Guid.CreateVersion7();
        var medicineId = Guid.CreateVersion7();
        var medicine = new AntibioticAnalysisItem(medicineId, "Amoxicillin", "Penicillin");
        var patient = CreatePatient(
            Guid.CreateVersion7(),
            plans:
            [
                CreatePlan(
                    Guid.CreateVersion7(),
                    doctorId,
                    PatientAnalysisConstants.AdherenceStates.Adhere,
                    [medicine]
                ),
            ]
        );
        var handler = CreateHandler(CreateBus(new PatientsByDoctorResult { Patients = [patient] }));

        var result = await handler.HandleAsync(
            new CreateAnalysisReportCommand
            {
                DoctorId = doctorId,
                Start = Start,
                End = End,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        var doctor = Assert.Single(result.Data.Doctors);
        var summary = Assert.Single(doctor.Medicines);
        Assert.Equal(medicineId, summary.MedicineId);
        Assert.Equal("Amoxicillin", summary.Name);
        Assert.Equal("Penicillin", summary.Classification);
        Assert.Equal(1, summary.TotalTreatmentPlans);
        Assert.Equal(1, summary.TotalAdherentPlans);
        Assert.Equal(0, summary.TotalNonAdherentPlans);
        Assert.Equal(0, summary.TotalPendingReviewPlans);
    }

    [Fact]
    public async Task Create_SameMedicineInTwoPlans_CountsDistinctPlans()
    {
        var doctorId = Guid.CreateVersion7();
        var medicineId = Guid.CreateVersion7();
        var medicine = new AntibioticAnalysisItem(medicineId, "Amoxicillin", "Penicillin");
        var patient = CreatePatient(
            Guid.CreateVersion7(),
            plans:
            [
                CreatePlan(
                    Guid.CreateVersion7(),
                    doctorId,
                    PatientAnalysisConstants.AdherenceStates.Adhere,
                    [medicine]
                ),
                CreatePlan(
                    Guid.CreateVersion7(),
                    doctorId,
                    PatientAnalysisConstants.AdherenceStates.Adhere,
                    [medicine]
                ),
            ]
        );
        var handler = CreateHandler(CreateBus(new PatientsByDoctorResult { Patients = [patient] }));

        var result = await handler.HandleAsync(
            new CreateAnalysisReportCommand
            {
                DoctorId = doctorId,
                Start = Start,
                End = End,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.TotalTreatmentPlans);
        var doctor = Assert.Single(result.Data.Doctors);
        Assert.Equal(2, doctor.TotalTreatmentPlans);
        var summary = Assert.Single(doctor.Medicines);
        Assert.Equal(2, summary.TotalTreatmentPlans);
        Assert.Equal(2, summary.TotalAdherentPlans);
    }

    [Fact]
    public async Task Create_SameMedicineTwiceInOnePlan_CountedOnce()
    {
        var doctorId = Guid.CreateVersion7();
        var medicineId = Guid.CreateVersion7();
        var medicine = new AntibioticAnalysisItem(medicineId, "Amoxicillin", "Penicillin");
        var patient = CreatePatient(
            Guid.CreateVersion7(),
            plans:
            [
                CreatePlan(
                    Guid.CreateVersion7(),
                    doctorId,
                    PatientAnalysisConstants.AdherenceStates.Adhere,
                    [medicine, medicine]
                ),
            ]
        );
        var handler = CreateHandler(CreateBus(new PatientsByDoctorResult { Patients = [patient] }));

        var result = await handler.HandleAsync(
            new CreateAnalysisReportCommand
            {
                DoctorId = doctorId,
                Start = Start,
                End = End,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.TotalTreatmentPlans);
        var doctor = Assert.Single(result.Data.Doctors);
        Assert.Equal(1, doctor.TotalTreatmentPlans);
        var summary = Assert.Single(doctor.Medicines);
        Assert.Equal(1, summary.TotalTreatmentPlans);
        Assert.Equal(1, summary.TotalAdherentPlans);
    }

    [Fact]
    public async Task Create_PlanSharedByTwoPatients_CountedOnce()
    {
        var doctorId = Guid.CreateVersion7();
        var sharedPlanId = Guid.CreateVersion7();
        var medicineId = Guid.CreateVersion7();
        var medicine = new AntibioticAnalysisItem(medicineId, "Amoxicillin", "Penicillin");
        var firstPatient = CreatePatient(
            Guid.CreateVersion7(),
            plans:
            [
                CreatePlan(
                    sharedPlanId,
                    doctorId,
                    PatientAnalysisConstants.AdherenceStates.Adhere,
                    [medicine]
                ),
            ]
        );
        var secondPatient = CreatePatient(
            Guid.CreateVersion7(),
            plans:
            [
                CreatePlan(
                    sharedPlanId,
                    doctorId,
                    PatientAnalysisConstants.AdherenceStates.Adhere,
                    [medicine]
                ),
            ]
        );
        var handler = CreateHandler(
            CreateBus(new PatientsByDoctorResult { Patients = [firstPatient, secondPatient] })
        );

        var result = await handler.HandleAsync(
            new CreateAnalysisReportCommand
            {
                DoctorId = doctorId,
                Start = Start,
                End = End,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.TotalPatients);
        Assert.Equal(1, result.Data.TotalTreatmentPlans);
        var doctor = Assert.Single(result.Data.Doctors);
        Assert.Equal(1, doctor.TotalTreatmentPlans);
        var summary = Assert.Single(doctor.Medicines);
        Assert.Equal(1, summary.TotalTreatmentPlans);
    }

    [Fact]
    public async Task Create_TwoDoctors_OrdersSummaryByDoctorId()
    {
        var firstDoctorId = Guid.CreateVersion7();
        var secondDoctorId = Guid.CreateVersion7();
        var expectedOrder = new[] { firstDoctorId, secondDoctorId }.OrderBy(id => id).ToArray();
        var patient = CreatePatient(
            Guid.CreateVersion7(),
            plans:
            [
                CreatePlan(
                    Guid.CreateVersion7(),
                    firstDoctorId,
                    PatientAnalysisConstants.AdherenceStates.Adhere
                ),
                CreatePlan(
                    Guid.CreateVersion7(),
                    secondDoctorId,
                    PatientAnalysisConstants.AdherenceStates.Adhere
                ),
            ]
        );
        var handler = CreateHandler(CreateBus(new PatientsByDoctorResult { Patients = [patient] }));

        var result = await handler.HandleAsync(
            new CreateAnalysisReportCommand
            {
                DoctorId = null,
                Start = Start,
                End = End,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Doctors.Count);
        Assert.Equal(expectedOrder[0], result.Data.Doctors.First().DoctorId);
        Assert.Equal(expectedOrder[1], result.Data.Doctors.Last().DoctorId);
    }

    [Fact]
    public async Task Create_ConvertsStartAndEndToUtc()
    {
        var localStart = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(3));
        var localEnd = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.FromHours(3));
        var handler = CreateHandler(CreateBus(new PatientsByDoctorResult()));

        var result = await handler.HandleAsync(
            new CreateAnalysisReportCommand
            {
                DoctorId = Guid.CreateVersion7(),
                Start = localStart,
                End = localEnd,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);
        Assert.Equal(localStart.ToUniversalTime(), result.Data.Start.ToUniversalTime());
        Assert.Equal(localEnd.ToUniversalTime(), result.Data.End.ToUniversalTime());

        var stored = await LoadAnalysisAsync(result.Data.AnalysisId);
        Assert.NotNull(stored);
        Assert.Equal(localStart.ToUniversalTime(), stored.Start.ToUniversalTime());
        Assert.Equal(localEnd.ToUniversalTime(), stored.End.ToUniversalTime());
    }

    [Fact]
    public async Task Create_PersistsDoctorIdAndCollectedIds()
    {
        var doctorId = Guid.CreateVersion7();
        var patientId = Guid.CreateVersion7();
        var planId = Guid.CreateVersion7();
        var patient = CreatePatient(
            patientId,
            plans: [CreatePlan(planId, doctorId, PatientAnalysisConstants.AdherenceStates.Adhere)]
        );
        var handler = CreateHandler(CreateBus(new PatientsByDoctorResult { Patients = [patient] }));

        var result = await handler.HandleAsync(
            new CreateAnalysisReportCommand
            {
                DoctorId = doctorId,
                Start = Start,
                End = End,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);

        var stored = await LoadAnalysisAsync(result.Data.AnalysisId);
        Assert.NotNull(stored);
        Assert.Equal(doctorId, stored.DoctorId);
        Assert.Contains(patientId.ToString(), stored.PatientIdsJson);
        Assert.Contains(planId.ToString(), stored.TreatmentPlanIdsJson);
    }

    [Fact]
    public async Task Create_ReportJsonRoundTripsWithSameCounts()
    {
        var doctorId = Guid.CreateVersion7();
        var medicineId = Guid.CreateVersion7();
        var medicine = new AntibioticAnalysisItem(medicineId, "Amoxicillin", "Penicillin");
        var patient = CreatePatient(
            Guid.CreateVersion7(),
            plans:
            [
                CreatePlan(
                    Guid.CreateVersion7(),
                    doctorId,
                    PatientAnalysisConstants.AdherenceStates.Adhere,
                    [medicine]
                ),
                CreatePlan(
                    Guid.CreateVersion7(),
                    doctorId,
                    PatientAnalysisConstants.AdherenceStates.NotAdhereValid
                ),
            ]
        );
        var handler = CreateHandler(CreateBus(new PatientsByDoctorResult { Patients = [patient] }));

        var result = await handler.HandleAsync(
            new CreateAnalysisReportCommand
            {
                DoctorId = doctorId,
                Start = Start,
                End = End,
            },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.NotNull(result.Data);

        var stored = await LoadAnalysisAsync(result.Data.AnalysisId);
        Assert.NotNull(stored);
        var roundTripped = JsonSerializer.Deserialize<AnalysisReportResult>(
            stored.ReportJson,
            JsonOptions
        );
        Assert.NotNull(roundTripped);
        Assert.Equal(result.Data.AnalysisId, roundTripped.AnalysisId);
        Assert.Equal(result.Data.DoctorId, roundTripped.DoctorId);
        Assert.Equal(result.Data.TotalPatients, roundTripped.TotalPatients);
        Assert.Equal(result.Data.TotalTreatmentPlans, roundTripped.TotalTreatmentPlans);
        Assert.Equal(result.Data.TotalAdherentPlans, roundTripped.TotalAdherentPlans);
        Assert.Equal(result.Data.TotalNonAdherentPlans, roundTripped.TotalNonAdherentPlans);
        Assert.Equal(result.Data.TotalPendingReviewPlans, roundTripped.TotalPendingReviewPlans);
        Assert.Equal(result.Data.Doctors.Count, roundTripped.Doctors.Count);
        Assert.Equal(result.Data.Doctors.First().Medicines.Count, roundTripped.Doctors.First().Medicines.Count);
    }

    private CreateAnalysisReportCommandHandler CreateHandler(Mock<IMessageBus> bus) =>
        new(bus.Object, _context);

    private static Mock<IMessageBus> CreateBus(PatientsByDoctorResult? result)
    {
        var bus = new Mock<IMessageBus>();
        bus.Setup(b =>
                b.InvokeAsync<PatientsByDoctorResult>(
                    It.IsAny<GetPatientsByDoctorQuery>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                )
            )
            .ReturnsAsync(result!);
        return bus;
    }

    private async Task<ClinicalAnalysis?> LoadAnalysisAsync(Guid analysisId)
    {
        await using var context = new AnalysisDbContext(_options);
        return await context
            .ClinicalAnalyses.IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == analysisId, TestContext.Current.CancellationToken);
    }

    private static PatientAnalysisItem CreatePatient(
        Guid id,
        string treatmentOutcome = PatientAnalysisConstants.TreatmentOutcomes.InTreatment,
        IEnumerable<TreatmentPlanAnalysisItem>? plans = null
    ) =>
        new()
        {
            Id = id,
            FullName = $"Patient {id}",
            PhoneNumber = "555-0100",
            DateOfBirth = new DateOnly(1990, 1, 1),
            Gender = "Male",
            Height = 175m,
            Weight = 70m,
            MedicalRecordCode = "MR-0001",
            HealthInsuranceCard = "HIC-0001",
            Address = "1 Main Street",
            City = "Springfield",
            Admission = new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero),
            Discharge = new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.Zero),
            TreatmentOutcome = treatmentOutcome,
            TreatmentPlans = plans ?? [],
        };

    private static TreatmentPlanAnalysisItem CreatePlan(
        Guid id,
        Guid doctorId,
        string adherenceState = PatientAnalysisConstants.AdherenceStates.Adhere,
        IEnumerable<AntibioticAnalysisItem>? medicines = null
    ) =>
        new(
            id,
            doctorId,
            adherenceState,
            "Active",
            new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero),
            null,
            medicines ?? []
        );
}

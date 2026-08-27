using Application.Features.EmpiricTreatmentProtocols.CreateEmpiricTreatmentProtocol;
using Domain.Enums;

namespace Application.Test.Features.EmpiricTreatmentProtocols.CreateEmpiricTreatmentProtocol;

public class CreateEmpiricTreatmentProtocolValidatorTest
{
    private readonly CreateEmpiricTreatmentProtocolValidator _validator = new();

    private static CreateEmpiricTreatmentProtocolCommand ValidCommand() => new()
    {
        Name = "IDSA/ATS 2024 CAP Empiric Guidance",
        Issuer = "Infectious Diseases Society of America",
        IssueDate = DateOnly.FromDateTime(DateTime.Today),
        Version = 1,
        DiseaseId = Guid.CreateVersion7(),
        Severity = Severity.Moderate,
        TreatmentSite = TreatmentSite.Inpatient,
        SpecialInfectionId = null,
        OtherCriteriaIds = [],
        MedicineIds = [Guid.CreateVersion7()],
    };

    # region Valid command

    [Fact]
    public async Task CreateEmpiricTreatmentProtocol_ValidCommand_Success()
    {
        var result = await _validator.ValidateAsync(ValidCommand(), TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task CreateEmpiricTreatmentProtocol_WithSpecialInfectionAndCriteria_Success()
    {
        var command = ValidCommand();
        command.SpecialInfectionId = Guid.CreateVersion7();
        command.OtherCriteriaIds = [Guid.CreateVersion7()];

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    // Boundary value technique: version 1 is the smallest valid value (must be > 0)
    [Fact]
    public async Task CreateEmpiricTreatmentProtocol_VersionBoundaryMin_Success()
    {
        var command = ValidCommand();
        command.Version = 1;

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    // Boundary value technique: issue date exactly today is the latest valid value
    [Fact]
    public async Task CreateEmpiricTreatmentProtocol_IssueDateBoundaryToday_Success()
    {
        var command = ValidCommand();
        command.IssueDate = DateOnly.FromDateTime(DateTime.Today);

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    [Theory]
    [InlineData("", "Name")]
    [InlineData("   ", "Name")]
    public async Task CreateEmpiricTreatmentProtocol_InvalidName_Fail(string name, string property)
    {
        var command = ValidCommand();
        command.Name = name;

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal(property, result.Errors[0].PropertyName);
    }

    [Theory]
    [InlineData("", "Issuer")]
    [InlineData("   ", "Issuer")]
    public async Task CreateEmpiricTreatmentProtocol_InvalidIssuer_Fail(string issuer, string property)
    {
        var command = ValidCommand();
        command.Issuer = issuer;

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal(property, result.Errors[0].PropertyName);
    }

    // Boundary value technique: any future issue date must be rejected
    [Fact]
    public async Task CreateEmpiricTreatmentProtocol_FutureIssueDate_Fail()
    {
        var command = ValidCommand();
        command.IssueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("IssueDate", result.Errors[0].PropertyName);
    }

    // Boundary value technique: version 0 is just below the valid range (must be > 0)
    [Fact]
    public async Task CreateEmpiricTreatmentProtocol_VersionZero_Fail()
    {
        var command = ValidCommand();
        command.Version = 0;

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("Version", result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task CreateEmpiricTreatmentProtocol_InvalidSeverity_Fail()
    {
        var command = ValidCommand();
        command.Severity = (Severity)99;

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("Severity", result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task CreateEmpiricTreatmentProtocol_InvalidTreatmentSite_Fail()
    {
        var command = ValidCommand();
        command.TreatmentSite = (TreatmentSite)5;

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("TreatmentSite", result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task CreateEmpiricTreatmentProtocol_EmptySpecialInfectionId_Fail()
    {
        var command = ValidCommand();
        command.SpecialInfectionId = Guid.Empty;

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("SpecialInfectionId", result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task CreateEmpiricTreatmentProtocol_EmptyMedicineIds_Fail()
    {
        var command = ValidCommand();
        command.MedicineIds = [];

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("MedicineIds", result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task CreateEmpiricTreatmentProtocol_MedicineIdEmpty_Fail()
    {
        var command = ValidCommand();
        command.MedicineIds = [Guid.Empty];

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        Assert.Contains(result.Errors, e => e.PropertyName.StartsWith("MedicineIds"));
    }

    [Fact]
    public async Task CreateEmpiricTreatmentProtocol_OtherCriteriaIdEmpty_Fail()
    {
        var command = ValidCommand();
        command.OtherCriteriaIds = [Guid.Empty];

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        Assert.Contains(result.Errors, e => e.PropertyName.StartsWith("OtherCriteriaIds"));
    }

    # endregion
}

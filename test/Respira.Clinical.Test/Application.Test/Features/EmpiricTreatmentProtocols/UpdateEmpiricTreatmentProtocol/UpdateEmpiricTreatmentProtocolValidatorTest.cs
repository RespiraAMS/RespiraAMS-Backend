using Application.Features.EmpiricTreatmentProtocols.UpdateEmpiricTreatmentProtocol;
using Domain.Enums;

namespace Application.Test.Features.EmpiricTreatmentProtocols.UpdateEmpiricTreatmentProtocol;

public class UpdateEmpiricTreatmentProtocolValidatorTest
{
    private readonly UpdateEmpiricTreatmentProtocolValidator _validator = new();

    private static UpdateEmpiricTreatmentProtocolCommand ValidCommand() => new()
    {
        Id = Guid.CreateVersion7(),
        Name = "IDSA/ATS 2024 CAP Empiric Guidance",
        Issuer = "Infectious Diseases Society of America",
        IssueDate = DateOnly.FromDateTime(DateTime.Today),
        Version = 1,
        Severity = Severity.Moderate,
        TreatmentSite = TreatmentSite.Inpatient,
        SpecialInfectionId = null,
        OtherCriteriaIds = [],
        MedicineIds = [Guid.CreateVersion7()],
    };

    # region Valid command

    [Fact]
    public async Task UpdateEmpiricTreatmentProtocol_ValidCommand_Success()
    {
        var result = await _validator.ValidateAsync(ValidCommand(), TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task UpdateEmpiricTreatmentProtocol_WithSpecialInfectionAndCriteria_Success()
    {
        var command = ValidCommand();
        command.SpecialInfectionId = Guid.CreateVersion7();
        command.OtherCriteriaIds = [Guid.CreateVersion7()];

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    // Boundary value technique: version 1 is the smallest valid value (must be > 0)
    [Fact]
    public async Task UpdateEmpiricTreatmentProtocol_VersionBoundaryMin_Success()
    {
        var command = ValidCommand();
        command.Version = 1;

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    // Boundary value technique: issue date exactly today is the latest valid value
    [Fact]
    public async Task UpdateEmpiricTreatmentProtocol_IssueDateBoundaryToday_Success()
    {
        var command = ValidCommand();
        command.IssueDate = DateOnly.FromDateTime(DateTime.Today);

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    [Fact]
    public async Task UpdateEmpiricTreatmentProtocol_EmptyId_Fail()
    {
        var command = ValidCommand();
        command.Id = Guid.Empty;

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("Id", result.Errors[0].PropertyName);
    }

    [Theory]
    [InlineData("", "Name")]
    [InlineData("   ", "Name")]
    public async Task UpdateEmpiricTreatmentProtocol_InvalidName_Fail(string name, string property)
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
    public async Task UpdateEmpiricTreatmentProtocol_InvalidIssuer_Fail(string issuer, string property)
    {
        var command = ValidCommand();
        command.Issuer = issuer;

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal(property, result.Errors[0].PropertyName);
    }

    // Boundary value technique: any future issue date must be rejected
    [Fact]
    public async Task UpdateEmpiricTreatmentProtocol_FutureIssueDate_Fail()
    {
        var command = ValidCommand();
        command.IssueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("IssueDate", result.Errors[0].PropertyName);
    }

    // Boundary value technique: version 0 is just below the valid range (must be > 0)
    [Fact]
    public async Task UpdateEmpiricTreatmentProtocol_VersionZero_Fail()
    {
        var command = ValidCommand();
        command.Version = 0;

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("Version", result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task UpdateEmpiricTreatmentProtocol_InvalidSeverity_Fail()
    {
        var command = ValidCommand();
        command.Severity = (Severity)99;

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("Severity", result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task UpdateEmpiricTreatmentProtocol_InvalidTreatmentSite_Fail()
    {
        var command = ValidCommand();
        command.TreatmentSite = (TreatmentSite)5;

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("TreatmentSite", result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task UpdateEmpiricTreatmentProtocol_EmptySpecialInfectionId_Fail()
    {
        var command = ValidCommand();
        command.SpecialInfectionId = Guid.Empty;

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("SpecialInfectionId", result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task UpdateEmpiricTreatmentProtocol_EmptyMedicineIds_Fail()
    {
        var command = ValidCommand();
        command.MedicineIds = [];

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal("MedicineIds", result.Errors[0].PropertyName);
    }

    [Fact]
    public async Task UpdateEmpiricTreatmentProtocol_MedicineIdEmpty_Fail()
    {
        var command = ValidCommand();
        command.MedicineIds = [Guid.Empty];

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        Assert.Contains(result.Errors, e => e.PropertyName.StartsWith("MedicineIds"));
    }

    [Fact]
    public async Task UpdateEmpiricTreatmentProtocol_OtherCriteriaIdEmpty_Fail()
    {
        var command = ValidCommand();
        command.OtherCriteriaIds = [Guid.Empty];

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        Assert.Contains(result.Errors, e => e.PropertyName.StartsWith("OtherCriteriaIds"));
    }

    # endregion
}

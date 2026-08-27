using Application.Features.Antibiotics.AddDosage;
using Domain.Enums;
using Range = Domain.Models.Range;

namespace Application.Test.Features.Antibiotics.AddDosage;

public class AddDosageValidatorTest
{
    private readonly AddDosageValidator _validator = new();

    # region Valid command

    [Theory]
    [InlineData(RouteOfAdministration.Oral, "500 mg orally every 8 hours")]
    [InlineData(RouteOfAdministration.Intravenous, "1 g IV every 8 hours")]
    public async Task AddDosage_StandardDose_Success(RouteOfAdministration route, string dose)
    {
        var result = await _validator.ValidateAsync(new AddDosageCommand
        {
            AntibioticId = Guid.CreateVersion7(),
            RouteOfAdministration = route,
            Dose = dose,
            Crcl = null,
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task AddDosage_AdjustedDoseWithCrcl_Success()
    {
        var result = await _validator.ValidateAsync(new AddDosageCommand
        {
            AntibioticId = Guid.CreateVersion7(),
            RouteOfAdministration = RouteOfAdministration.Intravenous,
            Dose = "500 mg IV every 12 hours",
            Crcl = new Range { Min = 15, IsMinExclusive = true, Max = 29, IsMaxExclusive = false, Unit = "mL/min" },
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    // Boundary of the RangeValidator rule "min <= max": min equal to max is still valid
    [Fact]
    public async Task AddDosage_CrclMinEqualsMax_Success()
    {
        var result = await _validator.ValidateAsync(new AddDosageCommand
        {
            AntibioticId = Guid.CreateVersion7(),
            RouteOfAdministration = RouteOfAdministration.Oral,
            Dose = "250 mg orally every 12 hours",
            Crcl = new Range { Min = 30, IsMinExclusive = false, Max = 30, IsMaxExclusive = false, Unit = "mL/min" },
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    // Null CrCl means standard dose: the nested RangeValidator must be skipped entirely
    // (a null range would otherwise break the min/max rules)
    [Fact]
    public async Task AddDosage_NullCrclSkipsRangeValidation_Success()
    {
        var result = await _validator.ValidateAsync(new AddDosageCommand
        {
            AntibioticId = Guid.CreateVersion7(),
            RouteOfAdministration = RouteOfAdministration.Oral,
            Dose = "500 mg orally every 8 hours",
            Crcl = null,
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    public static readonly TheoryData<Guid, int, string, Range?, string> InvalidCommands =
    [
        // Boundary: empty GUID violates NotEmpty on AntibioticId
        (Guid.Empty, 0, "500 mg orally every 8 hours", null, "AntibioticId"),
        // Boundary: 0 is a defined enum member (valid), 999 is outside every member
        (Guid.CreateVersion7(), 999, "500 mg orally every 8 hours", null, "RouteOfAdministration"),
        (Guid.CreateVersion7(), 0, "", null, "Dose"),
        (Guid.CreateVersion7(), 0, "   ", null, "Dose"),
        // Boundary of the RangeValidator rule "min <= max": min greater than max is invalid.
        // Realistic CrCl is measured in mL/min (normal adult kidney ~90-120)
        (
            Guid.CreateVersion7(), 0, "125 mg orally every 12 hours",
            new Range { Min = 60, IsMinExclusive = false, Max = 15, IsMaxExclusive = false, Unit = "mL/min" },
            "Crcl"
        ),
        (
            Guid.CreateVersion7(), 0, "125 mg orally every 12 hours",
            new Range { Min = 15, IsMinExclusive = false, Max = 60, IsMaxExclusive = false, Unit = "" },
            "Crcl.Unit"
        ),
    ];

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(InvalidCommands))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public async Task AddDosage_Fail(Guid antibioticId, int route, string dose, Range? crcl, string property)
    {
        var result = await _validator.ValidateAsync(new AddDosageCommand
        {
            AntibioticId = antibioticId,
            RouteOfAdministration = (RouteOfAdministration)route,
            Dose = dose,
            Crcl = crcl,
        }, TestContext.Current.CancellationToken);

        _ = Assert.Single(result.Errors);
        Assert.Equal(property, result.Errors[0].PropertyName);
    }

    # endregion
}

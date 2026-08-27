using Application.Features.Antibiotics.UpdateDosage;
using Domain.Enums;
using Range = Domain.Models.Range;

namespace Application.Test.Features.Antibiotics.UpdateDosage;

public class UpdateDosageValidatorTest
{
    private readonly UpdateDosageValidator _validator = new();

    # region Valid command

    [Theory]
    [InlineData("Oral", "500 mg orally every 8 hours")]
    [InlineData("Intravenous", "1 g IV every 8 hours")]
    public async Task UpdateDosage_StandardDose_Success(string route, string dose)
    {
        var result = await _validator.ValidateAsync(new UpdateDosageCommand
        {
            Id = Guid.CreateVersion7(),
            AntibioticId = Guid.CreateVersion7(),
            RouteOfAdministration = Enum.Parse<RouteOfAdministration>(route),
            Dose = dose,
            Crcl = null,
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    // Boundary of the RangeValidator rule "min <= max": min equal to max is still valid.
    // Realistic CrCl is measured in mL/min (normal adult kidney ~90-120)
    [Fact]
    public async Task UpdateDosage_AdjustedDoseWithCrcl_Success()
    {
        var result = await _validator.ValidateAsync(new UpdateDosageCommand
        {
            Id = Guid.CreateVersion7(),
            AntibioticId = Guid.CreateVersion7(),
            RouteOfAdministration = RouteOfAdministration.Intravenous,
            Dose = "500 mg IV every 12 hours",
            Crcl = new Range { Min = 30, IsMinExclusive = false, Max = 30, IsMaxExclusive = false, Unit = "mL/min" },
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    public static readonly TheoryData<Guid, Guid, int, string, Range?, string> InvalidCommands =
    [
        // Boundary: empty dosage GUID violates NotEmpty on Id
        (Guid.Empty, Guid.CreateVersion7(), 0, "500 mg orally every 8 hours", null, "Id"),
        // Boundary: empty dosage GUID violates NotEmpty on Id
        (Guid.CreateVersion7(), Guid.Empty, 0, "500 mg orally every 8 hours", null, "AntibioticId"),
        // Boundary: 999 is outside every defined enum member
        (Guid.CreateVersion7(), Guid.CreateVersion7(), 999, "500 mg orally every 8 hours", null, "RouteOfAdministration"),
        (Guid.CreateVersion7(), Guid.CreateVersion7(), 0, "", null, "Dose"),
        (Guid.CreateVersion7(), Guid.CreateVersion7(), 0, "   ", null, "Dose"),
        // Boundary of the RangeValidator rule "min <= max": min greater than max is invalid
        (
            Guid.CreateVersion7(), Guid.CreateVersion7(), 0, "125 mg orally every 12 hours",
            new Range { Min = 60, IsMinExclusive = false, Max = 15, IsMaxExclusive = false, Unit = "mL/min" },
            "Crcl"
        ),
    ];

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(InvalidCommands))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public async Task UpdateDosage_Fail(Guid id, Guid antibioticId, int route, string dose, Range? crcl, string property)
    {
        var result = await _validator.ValidateAsync(new UpdateDosageCommand
        {
            Id = id,
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

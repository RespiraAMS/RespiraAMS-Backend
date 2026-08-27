using Application.Features.Antibiograms.UpdateAntibiogram;
using Domain.Enums;

namespace Application.Test.Features.Antibiogram.UpdateAntibiogram;

public class UpdateAntibiogramValidatorTest
{
    private readonly UpdateAntibiogramValidator _validator = new();

    # region Valid command

    [Theory]
    [InlineData(MinimumInhibitoryConcentration.Susceptible)]
    [InlineData(MinimumInhibitoryConcentration.Intermediate)]
    [InlineData(MinimumInhibitoryConcentration.Resistance)]
    public async Task UpdateAntibiogram_Success(MinimumInhibitoryConcentration micLevel)
    {
        var result = await _validator.ValidateAsync(new UpdateAntibiogramCommand
        {
            Id = Guid.CreateVersion7(),
            MicLevel = micLevel,
            // Boundary: single-element lists are the lower bound of "not empty"
            MicIds = [Guid.CreateVersion7()],
            FirstPriorityMedicineIds = [Guid.CreateVersion7()],
            SecondPriorityMedicineIds = [Guid.CreateVersion7()],
        }, TestContext.Current.CancellationToken);

        Assert.Empty(result.Errors);
    }

    # endregion

    # region Invalid command

    public static readonly TheoryData<Guid, int, List<Guid>, List<Guid>, List<Guid>, string> InvalidCommands =
    [
        // Boundary: empty GUID violates NotEmpty on Id
        (Guid.Empty, 0, [Guid.CreateVersion7()], [Guid.CreateVersion7()], [Guid.CreateVersion7()], "Id"),
        // Boundary: 0 and 2 are defined enum members, 999 is outside every member
        (
            Guid.CreateVersion7(), 999,
            [Guid.CreateVersion7()], [Guid.CreateVersion7()], [Guid.CreateVersion7()], "MicLevel"
        ),
        // Boundary: empty list violates the collection-level NotEmpty
        (Guid.CreateVersion7(), 0, [], [Guid.CreateVersion7()], [Guid.CreateVersion7()], "MicIds"),
        // Boundary: one empty GUID inside the list violates RuleForEach NotEmpty,
        // which prefixes the index for collection items
        (
            Guid.CreateVersion7(), 0,
            [Guid.CreateVersion7(), Guid.Empty], [Guid.CreateVersion7()], [Guid.CreateVersion7()], "MicIds[1]"
        ),
        (Guid.CreateVersion7(), 0, [Guid.CreateVersion7()], [], [Guid.CreateVersion7()], "FirstPriorityMedicineIds"),
        (
            Guid.CreateVersion7(), 0,
            [Guid.CreateVersion7()], [Guid.Empty], [Guid.CreateVersion7()], "FirstPriorityMedicineIds[0]"
        ),
        (Guid.CreateVersion7(), 0, [Guid.CreateVersion7()], [Guid.CreateVersion7()], [], "SecondPriorityMedicineIds"),
        (
            Guid.CreateVersion7(), 0,
            [Guid.CreateVersion7()], [Guid.CreateVersion7()], [Guid.Empty], "SecondPriorityMedicineIds[0]"
        ),
    ];

    [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    [MemberData(nameof(InvalidCommands))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
    public async Task UpdateAntibiogram_Fail(Guid id, int micLevel, List<Guid> micIds,
        List<Guid> firstPriorityIds, List<Guid> secondPriorityIds, string property)
    {
        var result = await _validator.ValidateAsync(new UpdateAntibiogramCommand
        {
            Id = id,
            MicLevel = (MinimumInhibitoryConcentration)micLevel,
            MicIds = micIds,
            FirstPriorityMedicineIds = firstPriorityIds,
            SecondPriorityMedicineIds = secondPriorityIds,
        }, TestContext.Current.CancellationToken);

        Assert.Contains(result.Errors, x => x.PropertyName == property);
    }

    # endregion
}

using Application.Contracts.Mappers;
using Application.Features.Antibiotics.AddDosage;
using Domain.Enums;
using Domain.Models;
using Range = Domain.Models.Range;

namespace Application.Test.Features.Antibiotics.AddDosage;

public class AddDosageMapperTest
{
    private readonly ICreateMapper<Dosage, AddDosageCommand> _mapper = new AddDosageMapper();

    # region Happy path

    [Fact]
    public void ToModel_StandardDoseWithoutCrcl_Success()
    {
        var antibioticId = Guid.CreateVersion7();
        var command = new AddDosageCommand
        {
            AntibioticId = antibioticId,
            RouteOfAdministration = RouteOfAdministration.Oral,
            Dose = "500 mg orally every 8 hours",
            Crcl = null,
        };

        var model = _mapper.ToModel(command);

        // Base generates the ID so the handler can return it right after saving
        Assert.NotEqual(Guid.Empty, model.Id);
        Assert.Equal(antibioticId, model.AntibioticId);
        Assert.Equal(RouteOfAdministration.Oral, model.RouteOfAdministration);
        Assert.Equal("500 mg orally every 8 hours", model.Dose);
        Assert.Null(model.Crcl);
    }

    [Fact]
    public void ToModel_AdjustedDoseWithCrcl_Success()
    {
        var antibioticId = Guid.CreateVersion7();
        var crcl = new Range { Min = 15, IsMinExclusive = true, Max = 29, IsMaxExclusive = false, Unit = "mL/min" };
        var command = new AddDosageCommand
        {
            AntibioticId = antibioticId,
            RouteOfAdministration = RouteOfAdministration.Intravenous,
            Dose = "1 g IV every 12 hours",
            Crcl = crcl,
        };

        var model = _mapper.ToModel(command);

        Assert.NotEqual(Guid.Empty, model.Id);
        Assert.Equal(antibioticId, model.AntibioticId);
        Assert.Equal(RouteOfAdministration.Intravenous, model.RouteOfAdministration);
        Assert.Equal("1 g IV every 12 hours", model.Dose);

        // The CrCl range must be carried over by reference with all boundary flags intact
        Assert.NotNull(model.Crcl);
        Assert.Equal(crcl.Min, model.Crcl.Min);
        Assert.Equal(crcl.Max, model.Crcl.Max);
        Assert.Equal(crcl.IsMinExclusive, model.Crcl.IsMinExclusive);
        Assert.Equal(crcl.IsMaxExclusive, model.Crcl.IsMaxExclusive);
        Assert.Equal("mL/min", model.Crcl.Unit);
    }

    # endregion
}

using Application.Contracts.Mappers;
using Application.Features.Antibiotics.UpdateDosage;
using Domain.Enums;
using Domain.Models;
using Range = Domain.Models.Range;

namespace Application.Test.Features.Antibiotics.UpdateDosage;

public class UpdateDosageMapperTest
{
    private readonly IUpdateMapper<Dosage, UpdateDosageCommand> _mapper = new UpdateDosageMapper();

    # region Happy path

    [Fact]
    public void MapModel_AdjustedDoseWithCrcl_Success()
    {
        var before = DateTimeOffset.UtcNow.AddMinutes(-1);
        var dosage = new Dosage
        {
            AntibioticId = Guid.CreateVersion7(),
            RouteOfAdministration = RouteOfAdministration.Oral,
            Dose = "500 mg orally every 8 hours",
            Crcl = null,
        };
        var crcl = new Range { Min = 15, IsMinExclusive = true, Max = 29, IsMaxExclusive = false, Unit = "mL/min" };

        _mapper.MapModel(dosage, new UpdateDosageCommand
        {
            Id = dosage.Id,
            AntibioticId = dosage.AntibioticId,
            RouteOfAdministration = RouteOfAdministration.Intravenous,
            Dose = "1 g IV every 12 hours",
            Crcl = crcl,
        });

        Assert.Equal(RouteOfAdministration.Intravenous, dosage.RouteOfAdministration);
        Assert.Equal("1 g IV every 12 hours", dosage.Dose);

        // The CrCl range must be carried over with all boundary flags intact
        Assert.NotNull(dosage.Crcl);
        Assert.Equal(crcl.Min, dosage.Crcl.Min);
        Assert.Equal(crcl.Max, dosage.Crcl.Max);
        Assert.Equal(crcl.IsMinExclusive, dosage.Crcl.IsMinExclusive);
        Assert.Equal(crcl.IsMaxExclusive, dosage.Crcl.IsMaxExclusive);
        Assert.Equal("mL/min", dosage.Crcl.Unit);

        // The mapper stamps the update time and must not touch identity fields
        Assert.InRange(dosage.UpdatedAt, before, DateTimeOffset.UtcNow.AddSeconds(5));
        Assert.NotEqual(Guid.Empty, dosage.Id);
        Assert.Equal(dosage.AntibioticId, dosage.AntibioticId);
    }

    [Fact]
    public void MapModel_StandardDoseWithoutCrcl_Success()
    {
        // Turning an adjusted dose back into the standard dose: CrCl must be cleared
        var antibioticId = Guid.CreateVersion7();
        var dosage = new Dosage
        {
            AntibioticId = antibioticId,
            RouteOfAdministration = RouteOfAdministration.Oral,
            Dose = "250 mg orally every 12 hours",
            Crcl = new Range { Min = 15, IsMinExclusive = false, Max = 29, IsMaxExclusive = true, Unit = "mL/min" },
        };

        _mapper.MapModel(dosage, new UpdateDosageCommand
        {
            Id = dosage.Id,
            AntibioticId = antibioticId,
            RouteOfAdministration = RouteOfAdministration.Oral,
            Dose = "500 mg orally every 8 hours",
            Crcl = null,
        });

        Assert.Null(dosage.Crcl);
        Assert.Equal("500 mg orally every 8 hours", dosage.Dose);
        Assert.Equal(antibioticId, dosage.AntibioticId);
    }

    # endregion
}

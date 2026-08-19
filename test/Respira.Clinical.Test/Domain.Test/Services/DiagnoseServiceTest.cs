using Domain.Enums;
using Domain.Models;
using Domain.Services.Dtos;
using Domain.Services.Implementations;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.ServiceDefaults.Exceptions;
using Assert = Xunit.Assert;

namespace Domain.Test.Services;

/// <summary>
/// Since <see cref="DiagnoseService"/> marks all its method as protected, this fake
/// class is used for accessing these methods by expose the protected method as public
/// </summary>
/// <param name="logger"><see cref="DiagnoseService"/> logger</param>
public class MockDiagnoseService(ILogger<DiagnoseService> logger) : DiagnoseService(logger)
{
    public new decimal CrCl(int age, decimal weight, decimal height, decimal scr, bool isMale = true)
        => base.CrCl(age, weight, height, scr, isMale);

    public new bool NeedIcu(List<IcuHospitalizeCriterion> criteria, int scoreThreshold, List<Guid> options)
        => base.NeedIcu(criteria, scoreThreshold, options);

    public new IEnumerable<InfectionProbability> InfectionProbability(List<ResistanceRiskFactor> factors, List<Guid> options)
        => base.InfectionProbability(factors, options);

    public new List<Antibiotic> GetAdjustedDosage(List<Antibiotic> antibiotics, decimal crcl)
        => base.GetAdjustedDosage(antibiotics, crcl);

    public new List<Antibiotic> GetRecommendedMedicines(List<Antibiotic> antibiotics)
        => base.GetRecommendedMedicines(antibiotics);

    // DataNormalization is protected static -> accessible via the type name
    public new decimal DataNormalization(decimal value, decimal min, decimal max)
        => DiagnoseService.DataNormalization(value, min, max);
}

public class DiagnoseServiceTest
{
    private readonly MockDiagnoseService _service;

    public DiagnoseServiceTest()
    {
        var logger = new Mock<ILogger<DiagnoseService>>().Object;
        _service = new MockDiagnoseService(logger);
    }

    #region CrCl

    public static TheoryData<int, decimal, decimal, decimal, bool, decimal> CrclData => new()
    {
        { 50, 70, 1.7m, 1.0m, true, 87.5m },
        { 50, 70, 1.7m, 1.0m, false, 74.375m },
        { 86, 100, 1.0m, 1.0m, true, 40.6m },
        { 86, 100, 1.0m, 1.0m, false, 38.44m },
        { 86, 30, 1.0m, 1.0m, true, 20.65m },
    };

    public static TheoryData<int, decimal, decimal, decimal> CrclInvalidInputData => new()
    {
        { 50, 0, 1.7m, 1.0m },
        { 50, -70, 1.7m, 1.0m },
        { 50, 70, 0m, 1.0m },
        { 50, 70, 1.7m, 0m },
        { 50, 70, 1.7m, -1.0m },
    };

    [Theory]
    [MemberData(nameof(CrclData))]
    public void Crcl_ReturnsExpected(int age, decimal weight, decimal height, decimal scr, bool isMale, decimal expected)
    {
        var result = _service.CrCl(age, weight, height, scr, isMale);

        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(CrclInvalidInputData))]
    public void Crcl_InvalidInput_ThrowsBadRequest(int age, decimal weight, decimal height, decimal scr)
    {
        Assert.Throws<BadRequestException>(() => _service.CrCl(age, weight, height, scr));
    }

    #endregion

    #region NeedIcu

    private static readonly Guid CriterionA = Guid.NewGuid();
    private static readonly Guid CriterionB = Guid.NewGuid();
    private static readonly Guid UnknownCriterion = Guid.NewGuid();

    private static List<IcuHospitalizeCriterion> BuildIcuCriteria() =>
    [
        new() { DiseaseId = Guid.NewGuid(), CriterionId = CriterionA, Score = 2 },
        new() { DiseaseId = Guid.NewGuid(), CriterionId = CriterionB, Score = 3 },
    ];

    [Fact]
    public void NeedIcu_ScoreReachesThreshold_ReturnsTrue()
    {
        var criteria = BuildIcuCriteria();

        var result = _service.NeedIcu(criteria, 5, [CriterionA, CriterionB]);

        Assert.True(result);
    }

    [Fact]
    public void NeedIcu_ScoreBelowThreshold_ReturnsFalse()
    {
        var criteria = BuildIcuCriteria();

        var result = _service.NeedIcu(criteria, 5, [CriterionA]);

        Assert.False(result);
    }

    [Fact]
    public void NeedIcu_UnknownOption_IsIgnored()
    {
        var criteria = BuildIcuCriteria();

        var result = _service.NeedIcu(criteria, 5, [CriterionA, CriterionB, UnknownCriterion]);

        Assert.True(result);
    }

    [Fact]
    public void NeedIcu_NoOptions_ReturnsFalse()
    {
        var criteria = BuildIcuCriteria();

        var result = _service.NeedIcu(criteria, 5, []);

        Assert.False(result);
    }

    #endregion

    #region InfectionProbability

    private static readonly Guid PathogenIdP1 = Guid.NewGuid();
    private static readonly Guid PathogenIdP2 = Guid.NewGuid();
    private static readonly Guid CriterionCr1 = Guid.NewGuid();
    private static readonly Guid CriterionCr2 = Guid.NewGuid();
    private static readonly Guid CriterionCr3 = Guid.NewGuid();
    private static readonly Guid UnknownFactor = Guid.NewGuid();

    private static List<ResistanceRiskFactor> BuildFactors()
    {
        var p1 = new Pathogen { Name = "P1", Description = "Pathogen 1" };
        var p2 = new Pathogen { Name = "P2", Description = "Pathogen 2" };

        return
        [
            new ResistanceRiskFactor { DiseaseId = Guid.NewGuid(), Name = "F1", CriterionId = CriterionCr1, PathogenId = PathogenIdP1, Pathogen = p1 },
            new ResistanceRiskFactor { DiseaseId = Guid.NewGuid(), Name = "F2", CriterionId = CriterionCr2, PathogenId = PathogenIdP1, Pathogen = p1 },
            new ResistanceRiskFactor { DiseaseId = Guid.NewGuid(), Name = "F3", CriterionId = CriterionCr3, PathogenId = PathogenIdP2, Pathogen = p2 },
        ];
    }

    [Fact]
    public void InfectionProbability_AllMatchingFactors_ReturnsFullProbability()
    {
        var factors = BuildFactors();

        var result = _service.InfectionProbability(factors, [CriterionCr1, CriterionCr2, CriterionCr3]).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(1.0m, result.Single(x => x.Pathogen.Name == "P1").Probability);
        Assert.Equal(1.0m, result.Single(x => x.Pathogen.Name == "P2").Probability);
    }

    [Fact]
    public void InfectionProbability_PartialMatch_ReturnsRatio()
    {
        var factors = BuildFactors();

        var result = _service.InfectionProbability(factors, [CriterionCr1]).ToList();

        var p1 = Assert.Single(result);
        Assert.Equal(0.5m, p1.Probability);
    }

    [Fact]
    public void InfectionProbability_DuplicateOptions_AreDistinct()
    {
        var factors = BuildFactors();

        var result = _service.InfectionProbability(factors, [CriterionCr1, CriterionCr1]).ToList();

        var p1 = Assert.Single(result);
        Assert.Equal(0.5m, p1.Probability);
    }

    [Fact]
    public void InfectionProbability_UnknownOption_ThrowsBadRequest()
    {
        var factors = BuildFactors();

        Assert.Throws<BadRequestException>(() => _service.InfectionProbability(factors, [UnknownFactor]));
    }

    #endregion

    #region DataNormalization

    public static TheoryData<decimal, decimal, decimal, decimal> DataNormalizationData => new()
    {
        { 5, 0, 10, 0.5m },
        { 0, 0, 10, 0m },
        { 10, 0, 10, 1m },
        { 15, 0, 10, 1.5m },
        { 7, 7, 7, 0m },
    };

    [Theory]
    [MemberData(nameof(DataNormalizationData))]
    public void DataNormalization_ReturnsExpected(decimal value, decimal min, decimal max, decimal expected)
    {
        var result = _service.DataNormalization(value, min, max);

        Assert.Equal(expected, result);
    }

    #endregion

    #region GetAdjustedDosage

    private static Dosage StandardDose(string dose) => new()
    {
        AntibioticId = Guid.NewGuid(),
        RouteOfAdministration = RouteOfAdministration.Intravenous,
        Dose = dose,
        Crcl = null,
    };

    private static Dosage AdjustedDose(string dose, decimal min, decimal max, bool isMaxExclusive = false) => new()
    {
        AntibioticId = Guid.NewGuid(),
        RouteOfAdministration = RouteOfAdministration.Intravenous,
        Dose = dose,
        Crcl = new Models.Range
        {
            Min = min,
            IsMinExclusive = false,
            Max = max,
            IsMaxExclusive = isMaxExclusive,
            Unit = null,
        },
    };

    private static Antibiotic BuildAntibiotic(string name, params Dosage[] dosages)
    {
        var group = new AntibioticGroup { Name = "Group", Description = "Group", ParentId = null };
        return new Antibiotic
        {
            Name = name,
            AntibioticGroupId = group.Id,
            AntibioticGroup = group,
            Classification = AwareClassification.Access,
            Dosages = [.. dosages],
        };
    }

    [Fact]
    public void GetAdjustedDosage_CrclInRange_UsesAdjustedDose()
    {
        var ab = BuildAntibiotic("AB",
            StandardDose("standard"),
            AdjustedDose("low", 10, 30, isMaxExclusive: true),
            AdjustedDose("high", 30, 50));

        _service.GetAdjustedDosage([ab], 20);

        var dose = Assert.Single(ab.Dosages);
        Assert.Equal("low", dose.Dose);
    }

    [Fact]
    public void GetAdjustedDosage_CrclOnRangeBoundary_UsesMatchingDose()
    {
        var ab = BuildAntibiotic("AB",
            StandardDose("standard"),
            AdjustedDose("low", 10, 30, isMaxExclusive: true),
            AdjustedDose("high", 30, 50));

        _service.GetAdjustedDosage([ab], 30);

        var dose = Assert.Single(ab.Dosages);
        Assert.Equal("high", dose.Dose);
    }

    [Fact]
    public void GetAdjustedDosage_CrclOutOfRange_UsesStandardDose()
    {
        var ab = BuildAntibiotic("AB",
            StandardDose("standard"),
            AdjustedDose("low", 10, 30, isMaxExclusive: true),
            AdjustedDose("high", 30, 50));

        _service.GetAdjustedDosage([ab], 100);

        var dose = Assert.Single(ab.Dosages);
        Assert.Equal("standard", dose.Dose);
    }

    [Fact]
    public void GetAdjustedDosage_MultipleAntibiotics_AreFilteredIndependently()
    {
        var ab1 = BuildAntibiotic("AB1",
            StandardDose("standard"),
            AdjustedDose("low", 10, 30, isMaxExclusive: true));
        var ab2 = BuildAntibiotic("AB2",
            StandardDose("standard"),
            AdjustedDose("low", 10, 30, isMaxExclusive: true));

        _service.GetAdjustedDosage([ab1, ab2], 20);

        Assert.Equal("low", Assert.Single(ab1.Dosages).Dose);
        Assert.Equal("low", Assert.Single(ab2.Dosages).Dose);
    }

    #endregion

    #region GetRecommendedMedicines

    [Fact]
    public void GetRecommendedMedicines_PicksLowestClassificationPerGroup()
    {
        var group1 = new AntibioticGroup { Name = "G1", Description = "G1", ParentId = null };
        var group2 = new AntibioticGroup { Name = "G2", Description = "G2", ParentId = null };
        var watch = new Antibiotic
        {
            Name = "Watch",
            AntibioticGroupId = group1.Id,
            AntibioticGroup = group1,
            Classification = AwareClassification.Watch,
        };
        var access = new Antibiotic
        {
            Name = "Access",
            AntibioticGroupId = group1.Id,
            AntibioticGroup = group1,
            Classification = AwareClassification.Access,
        };
        var reserve = new Antibiotic
        {
            Name = "Reserve",
            AntibioticGroupId = group2.Id,
            AntibioticGroup = group2,
            Classification = AwareClassification.Reserve,
        };

        var result = _service.GetRecommendedMedicines([watch, access, reserve]);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Name == "Access");
        Assert.Contains(result, x => x.Name == "Reserve");
        Assert.DoesNotContain(result, x => x.Name == "Watch");
    }

    [Fact]
    public void GetRecommendedMedicines_EmptyList_ReturnsEmpty()
    {
        var result = _service.GetRecommendedMedicines([]);

        Assert.Empty(result);
    }

    #endregion
}

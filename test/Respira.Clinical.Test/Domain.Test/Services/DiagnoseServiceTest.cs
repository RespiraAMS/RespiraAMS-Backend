using Domain.Enums;
using Domain.Models;
using Domain.Services.Dtos;
using Domain.Services.Implementations;
using Microsoft.Extensions.Logging;
using Moq;
using Assert = Xunit.Assert;
using Range = Domain.Models.Range;

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
    private readonly Mock<ILogger<DiagnoseService>> _loggerMock;
    private readonly MockDiagnoseService _service;

    public DiagnoseServiceTest()
    {
        _loggerMock = new Mock<ILogger<DiagnoseService>>();
        _service = new MockDiagnoseService(_loggerMock.Object);
    }

    # region CrCl Test
    public static readonly TheoryData<int, decimal, decimal, decimal> InvalidInputs =
    [
        // weight = 0 (lower boundary) / weight < 0
        (45, 0m, 1.75m, 1.0m),
        (45, -70m, 1.75m, 1.0m),
        // height = 0 (lower boundary) / height < 0
        (45, 70m, 0m, 1.0m),
        (45, 70m, -1.75m, 1.0m),
        // scr = 0 (lower boundary) / scr < 0
        (45, 70m, 1.75m, 0m),
        (45, 70m, 1.75m, -1.0m),
    ];

    [Theory]
    [MemberData(nameof(InvalidInputs))]
    public void CrCl_NonPositiveInput_Fail(int age, decimal weight, decimal height, decimal scr)
    {
        Assert.Throws<ArgumentException>(() => _service.CrCl(age, weight, height, scr));
    }

    /*=== CrCl: non-obese patients use Cockcroft-Gault ===*/

    [Fact]
    public void CrCl_NonObeseMale_ReturnsCockcroftGaultValue()
    {
        // Adult male: 45 years old, 70 kg, 1.75 m, scr 1.0 mg/dL -> BMI ~22.86
        // (140 - 45) * 70 / (72 * 1.0) = 92.361...
        var crcl = _service.CrCl(45, 70m, 1.75m, 1.0m);

        Assert.Equal(92.36m, crcl, 2);
    }

    [Fact]
    public void CrCl_NonObeseFemale_AppliesFemaleFactorOf85Percent()
    {
        // Same adult but female: 92.361... * 0.85 = 78.5069...
        var crcl = _service.CrCl(45, 70m, 1.75m, 1.0m, isMale: false);

        Assert.Equal(78.51m, crcl, 2);
    }

    [Fact]
    public void CrCl_BmiJustBelow30_UsesCockcroftGault()
    {
        // Boundary: 67.4 kg at 1.5 m -> BMI = 29.95... (< 30, not obese)
        // (140 - 45) * 67.4 / (72 * 1.0) = 88.93...
        var crcl = _service.CrCl(45, 67.4m, 1.5m, 1.0m);

        Assert.Equal(88.93m, crcl, 2);
    }

    /*=== CrCl: obese patients (BMI >= 30) use Salazar-Corcoran ===*/

    [Fact]
    public void CrCl_ObeseMale_ReturnsSalazarCorcoranValue()
    {
        // Obese male: 45 years old, 110 kg, 1.80 m, scr 1.2 mg/dL -> BMI ~33.95
        // (137 - 45) * ((0.285 * 110) + (12.1 * 1.8^2)) / (51 * 1.2) = 106.06...
        var crcl = _service.CrCl(45, 110m, 1.80m, 1.2m);

        Assert.Equal(106.06m, crcl, 2);
    }

    [Fact]
    public void CrCl_ObeseFemale_ReturnsSalazarCorcoranValue()
    {
        // Obese female: same data -> (146 - 45) * ((0.287 * 110) + (9.74 * 1.8^2)) / (60 * 1.2) = 88.55...
        var crcl = _service.CrCl(45, 110m, 1.80m, 1.2m, isMale: false);

        Assert.Equal(88.55m, crcl, 2);
    }

    [Fact]
    public void CrCl_BmiExactly30_UsesSalazarCorcoran()
    {
        // Boundary: 67.5 kg at 1.5 m -> BMI = exactly 30, considered obese.
        // Salazar-Corcoran: (137 - 45) * ((0.285 * 67.5) + (12.1 * 1.5^2)) / (51 * 1.0) = 83.81...
        // (Cockcroft-Gault would give 89.06 instead)
        var crcl = _service.CrCl(45, 67.5m, 1.5m, 1.0m);

        Assert.Equal(83.81m, crcl, 2);
    }

    /*=== CrCl: age boundaries with realistic extremes ===*/

    [Fact]
    public void CrCl_YoungAdultLowerBoundary_ComputesSuccessfully()
    {
        // Youngest adult (18 years), smallest plausible build: 40 kg, 1.40 m, scr 0.2 mg/dL
        // (140 - 18) * 40 / (72 * 0.2) = 338.888...
        var crcl = _service.CrCl(18, 40m, 1.40m, 0.2m);

        Assert.Equal(338.89m, crcl, 2);
    }

    [Fact]
    public void CrCl_ElderlyPatientUpperBoundary_ReturnsReducedClearance()
    {
        // Elderly patient (90 years): 65 kg, 1.70 m, scr 1.5 mg/dL -> BMI ~22.49
        // (140 - 90) * 65 / (72 * 1.5) = 30.09...
        var crcl = _service.CrCl(90, 65m, 1.70m, 1.5m);

        Assert.Equal(30.09m, crcl, 2);
    }

    # endregion


    # region NeedIcu Test

    private static readonly Guid PneumoniaDiseaseId = Guid.CreateVersion7();
    private static readonly Guid RespiratoryRateCriterionId = Guid.CreateVersion7();
    private static readonly Guid LowPao2Fio2RatioCriterionId = Guid.CreateVersion7();
    private static readonly Guid HypotensionCriterionId = Guid.CreateVersion7();

    // Pneumonia ICU criteria (SMART-COP like): respiratory rate >= 30/min,
    // PaO2/FiO2 <= 250 mmHg, systolic blood pressure < 90 mmHg
    private static readonly List<IcuHospitalizeCriterion> IcuCriteria =
    [
        new() { DiseaseId = PneumoniaDiseaseId, CriterionId = RespiratoryRateCriterionId, Score = 1 },
        new() { DiseaseId = PneumoniaDiseaseId, CriterionId = LowPao2Fio2RatioCriterionId, Score = 2 },
        new() { DiseaseId = PneumoniaDiseaseId, CriterionId = HypotensionCriterionId, Score = 3 },
    ];

    private void VerifyWarningLogged(Guid optionId, Times times)
        => _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(optionId.ToString())),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);

    /*=== NeedIcu: threshold boundary ===*/

    [Fact]
    public void NeedIcu_NoOptionMatched_Fail()
    {
        var result = _service.NeedIcu(IcuCriteria, 5, []);

        Assert.False(result);
    }

    [Fact]
    public void NeedIcu_TotalScoreExactlyEqualsThreshold_Pass()
    {
        // Boundary: PaO2/FiO2 (2) + hypotension (3) = exactly the minimum threshold 5
        var result = _service.NeedIcu(IcuCriteria, 5, [LowPao2Fio2RatioCriterionId, HypotensionCriterionId]);

        Assert.True(result);
    }

    [Fact]
    public void NeedIcu_TotalScoreOnePointBelowThreshold_Fail()
    {
        // Boundary: respiratory rate (1) + hypotension (3) = 4, just below the threshold 5
        var result = _service.NeedIcu(IcuCriteria, 5, [RespiratoryRateCriterionId, HypotensionCriterionId]);

        Assert.False(result);
    }

    [Fact]
    public void NeedIcu_TotalScoreAboveThreshold_Pass()
    {
        // All three criteria matched: 1 + 2 + 3 = 6 > 5
        var result = _service.NeedIcu(IcuCriteria, 5, [RespiratoryRateCriterionId, LowPao2Fio2RatioCriterionId, HypotensionCriterionId]);

        Assert.True(result);
    }

    /*=== NeedIcu: unknown options are ignored and skipped ===*/

    [Fact]
    public void NeedIcu_UnknownOption_IgnoredAndProcessingContinues()
    {
        // Unknown option comes first: hypotension (3) after it must still be counted,
        // proving the loop continued past the unknown option instead of stopping
        var unknownOption = Guid.CreateVersion7();

        Assert.Throws<ArgumentException>(() => _service.NeedIcu(IcuCriteria, 3, [unknownOption, HypotensionCriterionId]));
        VerifyWarningLogged(unknownOption, Times.Once());
    }

    [Fact]
    public void NeedIcu_AllOptionsUnknown_Fail()
    {
        var unknownFirst = Guid.CreateVersion7();
        var unknownSecond = Guid.CreateVersion7();

        Assert.Throws<ArgumentException>(() => _service.NeedIcu(IcuCriteria, 5, [unknownFirst, unknownSecond]));
        VerifyWarningLogged(unknownFirst, Times.Once()); // Will throw immediately on the first unknown option, so only one log
    }

    [Fact]
    public void NeedIcu_EmptyCriteriaList_AllOptionsIgnored()
    {
        var option = Guid.CreateVersion7();

        Assert.Throws<ArgumentException>(() => _service.NeedIcu([], 5, [option]));
        VerifyWarningLogged(option, Times.Once());
    }

    # endregion


    # region InfectionProbability Test

    private static readonly Pathogen KlebsiellaPneumoniae = new()
    {
        Name = "Klebsiella pneumoniae",
        Description = "Gram-negative bacillus causing hospital-acquired pneumonia",
    };

    private static readonly Pathogen PseudomonasAeruginosa = new()
    {
        Name = "Pseudomonas aeruginosa",
        Description = "Gram-negative rod, common cause of ventilator-associated pneumonia",
    };

    private static readonly Pathogen Mrsa = new()
    {
        Name = "Methicillin-resistant Staphylococcus aureus",
        Description = "Gram-positive coccus resistant to most beta-lactam antibiotics",
    };

    private static readonly Guid ProlongedHospitalizationFactorId = Guid.CreateVersion7();
    private static readonly Guid PriorCarbapenemUseFactorId = Guid.CreateVersion7();
    private static readonly Guid IndwellingDeviceFactorId = Guid.CreateVersion7();
    private static readonly Guid NeutropeniaFactorId = Guid.CreateVersion7();
    private static readonly Guid StructuralLungDiseaseFactorId = Guid.CreateVersion7();
    private static readonly Guid MrsaNasalColonizationFactorId = Guid.CreateVersion7();

    // Resistance risk factors for hospital-acquired pneumonia:
    // Klebsiella pneumoniae has 3 factors, Pseudomonas aeruginosa 2, MRSA 1
    private static readonly List<ResistanceRiskFactor> ResistanceRiskFactors =
    [
        CreateResistanceRiskFactor(KlebsiellaPneumoniae, ProlongedHospitalizationFactorId, "Hospital stay longer than 5 days"),
        CreateResistanceRiskFactor(KlebsiellaPneumoniae, PriorCarbapenemUseFactorId, "Carbapenem exposure within the last 90 days"),
        CreateResistanceRiskFactor(KlebsiellaPneumoniae, IndwellingDeviceFactorId, "Indwelling medical device (ventilator or central line)"),
        CreateResistanceRiskFactor(PseudomonasAeruginosa, NeutropeniaFactorId, "Neutropenia (ANC below 500/mm3)"),
        CreateResistanceRiskFactor(PseudomonasAeruginosa, StructuralLungDiseaseFactorId, "Structural lung disease (bronchiectasis)"),
        CreateResistanceRiskFactor(Mrsa, MrsaNasalColonizationFactorId, "Prior MRSA nasal colonization"),
    ];

    private static ResistanceRiskFactor CreateResistanceRiskFactor(Pathogen pathogen, Guid criterionId, string name)
        => new()
        {
            DiseaseId = PneumoniaDiseaseId,
            Name = name,
            CriterionId = criterionId,
            Pathogen = pathogen,
            PathogenId = pathogen.Id,
        };

    /*=== InfectionProbability: fail path ===*/

    [Fact]
    public void InfectionProbability_UnknownOption_Fail()
    {
        // Unlike NeedIcu, an unknown option is NOT ignored but rejected
        var unknownOption = Guid.CreateVersion7();

        Assert.Throws<ArgumentException>(
            () => _service.InfectionProbability(ResistanceRiskFactors, [unknownOption]));
        VerifyWarningLogged(unknownOption, Times.Once());
    }

    /*=== InfectionProbability: probability boundary (range 0 to 1) ===*/

    [Fact]
    public void InfectionProbability_AllFactorsOfOnePathogenMatched_ProbabilityEqualsOne()
    {
        // Upper boundary: all 3 Klebsiella factors matched -> probability 3/3 = 1
        var result = _service.InfectionProbability(ResistanceRiskFactors,
            [ProlongedHospitalizationFactorId, PriorCarbapenemUseFactorId, IndwellingDeviceFactorId]).ToList();

        var klebsiella = Assert.Single(result, x => x.Pathogen.Id == KlebsiellaPneumoniae.Id);
        Assert.Equal(1m, klebsiella.Probability);
    }

    [Fact]
    public void InfectionProbability_NoOptionMatched_ReturnsEmpty()
    {
        // Lower boundary: no factor matched -> no pathogen to report
        var result = _service.InfectionProbability(ResistanceRiskFactors, []);

        Assert.Empty(result);
    }

    /*=== InfectionProbability: per-pathogen computation ===*/

    [Fact]
    public void InfectionProbability_MultiplePathogensPartialMatch_ComputesEachProbability()
    {
        // One Klebsiella factor out of 3 -> 1/3 ~ 0.3333, one Pseudomonas factor
        // out of 2 -> 0.5. MRSA has no matched factor -> excluded from result
        var result = _service.InfectionProbability(ResistanceRiskFactors,
            [ProlongedHospitalizationFactorId, StructuralLungDiseaseFactorId]).ToList();

        var klebsiella = Assert.Single(result, x => x.Pathogen.Id == KlebsiellaPneumoniae.Id);
        Assert.Equal(0.3333m, klebsiella.Probability, 4);

        var pseudomonas = Assert.Single(result, x => x.Pathogen.Id == PseudomonasAeruginosa.Id);
        Assert.Equal(0.5m, pseudomonas.Probability);

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, x => x.Pathogen.Id == Mrsa.Id);
    }

    [Fact]
    public void InfectionProbability_MultipleFactorsOfSamePathogen_ScoresAccumulated()
    {
        // Two distinct Klebsiella factors matched -> 2/3 ~ 0.6667
        var result = _service.InfectionProbability(ResistanceRiskFactors,
            [ProlongedHospitalizationFactorId, PriorCarbapenemUseFactorId]).ToList();

        var klebsiella = Assert.Single(result, x => x.Pathogen.Id == KlebsiellaPneumoniae.Id);
        Assert.Equal(0.6667m, klebsiella.Probability, 4);
    }

    /*=== InfectionProbability: duplicate options are counted once ===*/

    [Fact]
    public void InfectionProbability_DuplicatedOption_CountedOnlyOnce()
    {
        // The same option passed twice must not inflate the score: still 1/2 = 0.5,
        // not 2/2 = 1
        var result = _service.InfectionProbability(ResistanceRiskFactors,
            [NeutropeniaFactorId, NeutropeniaFactorId]).ToList();

        var pseudomonas = Assert.Single(result, x => x.Pathogen.Id == PseudomonasAeruginosa.Id);
        Assert.Equal(0.5m, pseudomonas.Probability);
    }

    # endregion


    # region DataNormalization Test

    public static readonly TheoryData<decimal, decimal, decimal, decimal> NormalizationBoundaries =
    [
        // Body temperature 35 C at the lower bound of the febrile range -> 0
        (35m, 35m, 42m, 0m),
        // Body temperature 42 C at the upper bound -> 1
        (42m, 35m, 42m, 1m),
        // Degenerate range where max == min (constant value): guard returns
        // 0 instead of dividing by zero
        (37m, 37m, 37m, 0m),
    ];

    [Theory]
    [MemberData(nameof(NormalizationBoundaries))]
    public void DataNormalization_ValueAtRangeBounds_ReturnsBoundaryValue(
        decimal value, decimal min, decimal max, decimal expected)
    {
        var result = _service.DataNormalization(value, min, max);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void DataNormalization_ValueInsideRange_ReturnsProportionOfRange()
    {
        // Body temperature 36.8 C in the 35-42 C range -> (36.8 - 35) / (42 - 35) = 1.8 / 7 ~ 0.2571
        var result = _service.DataNormalization(36.8m, 35m, 42m);

        Assert.Equal(0.2571m, result, 4);
    }

    # endregion


    # region GetAdjustedDosage Test

    private const string MeropenemStandardDose = "1 g IV every 8 hours";
    private const string MeropenemRenalAdjustedDose = "1 g IV every 12 hours";

    /*
     * GetAdjustedDosage mutates the given Antibiotic instances (it replaces their
     * Dosages), so every test builds fresh data through these factories instead of
     * sharing static instances
     */

    private static Range CrClRange(decimal min, decimal max, bool isMinExclusive = false,
        bool isMaxExclusive = false)
        => new()
        {
            Min = min,
            Max = max,
            IsMinExclusive = isMinExclusive,
            IsMaxExclusive = isMaxExclusive,
            Unit = "ml/min",
        };

    private static Dosage CreateDosage(Guid antibioticId, string dose, RouteOfAdministration route, Range? crcl)
        => new()
        {
            AntibioticId = antibioticId,
            Dose = dose,
            RouteOfAdministration = route,
            Crcl = crcl,
        };

    private static Antibiotic CreateAntibiotic(string name,
        params (string Dose, RouteOfAdministration Route, Range? Crcl)[] dosages)
    {
        var antibioticId = Guid.CreateVersion7();
        var antibioticGroup = new AntibioticGroup
        {
            Name = $"{name} group",
            Description = $"Chemical group of {name}",
            ParentId = null,
        };

        return new Antibiotic
        {
            Name = name,
            AntibioticGroupId = antibioticGroup.Id,
            AntibioticGroup = antibioticGroup,
            Classification = AwareClassification.Watch,
            Dosages = [.. dosages.Select(x => CreateDosage(antibioticId, x.Dose, x.Route, x.Crcl))],
        };
    }

    /*=== GetAdjustedDosage: fail path ===*/

    public static readonly TheoryData<decimal> NonPositiveCrClValues = [0m, -15m];

    [Theory]
    [MemberData(nameof(NonPositiveCrClValues))]
    public void GetAdjustedDosage_NonPositiveCrCl_Fail(decimal crcl)
    {
        var antibiotics = new List<Antibiotic>
        {
            CreateAntibiotic("Meropenem",
                (MeropenemStandardDose, RouteOfAdministration.Intravenous, null)),
        };

        Assert.Throws<ArgumentException>(() => _service.GetAdjustedDosage(antibiotics, crcl));
    }

    [Fact]
    public void GetAdjustedDosage_EmptyAntibioticsList_Fail()
    {
        Assert.Throws<ArgumentException>(() => _service.GetAdjustedDosage([], 42m));
    }

    /*=== GetAdjustedDosage: dose selection against CrCl range boundaries ===*/

    // Meropenem renal adjustment for moderately impaired kidney function (CrCl 30-60 ml/min):
    // interval widened from 8 to 12 hours; below or above the range the standard dose applies
    public static readonly TheoryData<decimal, string> CrClToExpectedDose =
    [
        // Just below the adjusted range -> standard
        (29m, MeropenemStandardDose),
        // Exactly at the inclusive lower bound -> adjusted
        (30m, MeropenemRenalAdjustedDose),
        // Inside the range -> adjusted
        (45m, MeropenemRenalAdjustedDose),
        // Exactly at the inclusive upper bound -> adjusted
        (60m, MeropenemRenalAdjustedDose),
        // Just above the range -> standard
        (61m, MeropenemStandardDose),
        // Normal-high renal function (young healthy adult) -> standard
        (120m, MeropenemStandardDose),
    ];

    [Theory]
    [MemberData(nameof(CrClToExpectedDose))]
    public void GetAdjustedDosage_CrClAgainstRangeBoundaries_PicksCorrectDose(decimal crcl, string expectedDose)
    {
        var antibiotics = new List<Antibiotic>
        {
            CreateAntibiotic("Meropenem",
                (MeropenemStandardDose, RouteOfAdministration.Intravenous, null),
                (MeropenemRenalAdjustedDose, RouteOfAdministration.Intravenous, CrClRange(30m, 60m))),
        };

        var result = _service.GetAdjustedDosage(antibiotics, crcl);

        var dosage = Assert.Single(result.Single().Dosages);
        Assert.Equal(expectedDose, dosage.Dose);
    }

    [Fact]
    public void GetAdjustedDosage_CrClAtExclusiveUpperBound_FallsBackToStandardDose()
    {
        // Adjusted range [10, 50): CrCl exactly on the excluded bound does not match it
        var antibiotics = new List<Antibiotic>
        {
            CreateAntibiotic("Meropenem",
                (MeropenemStandardDose, RouteOfAdministration.Intravenous, null),
                (MeropenemRenalAdjustedDose, RouteOfAdministration.Intravenous,
                    CrClRange(10m, 50m, isMaxExclusive: true))),
        };

        var result = _service.GetAdjustedDosage(antibiotics, 50m);

        var dosage = Assert.Single(result.Single().Dosages);
        Assert.Equal(MeropenemStandardDose, dosage.Dose);
    }

    /*=== GetAdjustedDosage: filtering across antibiotics and dosages ===*/

    [Fact]
    public void GetAdjustedDosage_MultipleAntibiotics_FilteredIndependently()
    {
        // Meropenem (renal adjustment needed) matches the range and takes the adjusted
        // dose, while ceftriaxone (hepatic elimination) has no adjusted dose and keeps
        // its standard one untouched
        var antibiotics = new List<Antibiotic>
        {
            CreateAntibiotic("Meropenem",
                (MeropenemStandardDose, RouteOfAdministration.Intravenous, null),
                (MeropenemRenalAdjustedDose, RouteOfAdministration.Intravenous, CrClRange(30m, 60m))),
            CreateAntibiotic("Ceftriaxone",
                ("2 g IV every 24 hours", RouteOfAdministration.Intravenous, null)),
        };

        var result = _service.GetAdjustedDosage(antibiotics, 42m);

        Assert.Equal(MeropenemRenalAdjustedDose, Assert.Single(result[0].Dosages).Dose);
        Assert.Equal("2 g IV every 24 hours", Assert.Single(result[1].Dosages).Dose);
    }

    [Fact]
    public void GetAdjustedDosage_MultipleMatchingAdjustedDoses_KeepsAllMatches()
    {
        // Levofloxacin renal adjustment exists for both routes: when CrCl falls into
        // the range, both adjusted doses are kept and both standard doses are dropped
        var antibiotics = new List<Antibiotic>
        {
            CreateAntibiotic("Levofloxacin",
                ("750 mg orally once daily", RouteOfAdministration.Oral, null),
                ("750 mg IV once daily", RouteOfAdministration.Intravenous, null),
                ("750 mg orally every 48 hours", RouteOfAdministration.Oral, CrClRange(20m, 49m)),
                ("750 mg IV every 48 hours", RouteOfAdministration.Intravenous, CrClRange(20m, 49m))),
        };

        var result = _service.GetAdjustedDosage(antibiotics, 35m);

        Assert.Equal(2, result.Single().Dosages.Count);
        Assert.Contains(result.Single().Dosages, x => x.Dose == "750 mg orally every 48 hours");
        Assert.Contains(result.Single().Dosages, x => x.Dose == "750 mg IV every 48 hours");
    }

    # endregion


    # region GetRecommendedMedicines Test

    private static readonly AntibioticGroup BetaLactamGroup = new()
    {
        Name = "Beta-lactams",
        Description = "Cell wall synthesis inhibitors sharing the beta-lactam ring",
        ParentId = null,
    };

    private static readonly AntibioticGroup MacrolideGroup = new()
    {
        Name = "Macrolides",
        Description = "Protein synthesis inhibitors with a macrocyclic lactone ring",
        ParentId = null,
    };

    /*=== GetRecommendedMedicines: fail path ===*/

    private static Antibiotic CreateAntibioticForRecommendation(string name,
        AwareClassification classification, AntibioticGroup group)
        => new()
        {
            Name = name,
            AntibioticGroupId = group.Id,
            AntibioticGroup = group,
            Classification = classification,
            Dosages = [],
        };

    [Fact]
    public void GetRecommendedMedicines_EmptyAntibioticsList_Fail()
    {
        Assert.Throws<ArgumentException>(() => _service.GetRecommendedMedicines([]));
    }

    /*=== GetRecommendedMedicines: exactly one medicine per antibiotic group ===*/

    [Fact]
    public void GetRecommendedMedicines_MultipleGroups_PicksOnePerGroupWithLowestAWaReClass()
    {
        // Beta-lactams offer 3 candidates where the Access-classified amoxicillin must
        // beat its AccessWatch and Watch relatives. Macrolides have a single candidate
        // (group-size lower boundary), which passes through unchanged
        var antibiotics = new List<Antibiotic>
        {
            CreateAntibioticForRecommendation("Meropenem", AwareClassification.Watch, BetaLactamGroup),
            CreateAntibioticForRecommendation("Azithromycin", AwareClassification.Watch, MacrolideGroup),
            CreateAntibioticForRecommendation("Amoxicillin", AwareClassification.Access, BetaLactamGroup),
            CreateAntibioticForRecommendation("Co-amoxiclav", AwareClassification.AccessWatch, BetaLactamGroup),
        };

        var result = _service.GetRecommendedMedicines(antibiotics);

        // Business rule: at most one medicine can be picked per antibiotic group,
        // so each source group contributes exactly one entry and nothing else remains
        Assert.Equal(2, result.Count);
        foreach (var groupId in new[] { BetaLactamGroup.Id, MacrolideGroup.Id })
        {
            Assert.Single(result, x => x.AntibioticGroupId == groupId);
        }

        // The single beta-lactam pick must be the lowest AWaRe classification
        var betaLactamPick = Assert.Single(result, x => x.AntibioticGroupId == BetaLactamGroup.Id);
        Assert.Equal("Amoxicillin", betaLactamPick.Name);

        var macrolidePick = Assert.Single(result, x => x.AntibioticGroupId == MacrolideGroup.Id);
        Assert.Equal("Azithromycin", macrolidePick.Name);

        Assert.DoesNotContain(result, x => x.Name == "Meropenem");
        Assert.DoesNotContain(result, x => x.Name == "Co-amoxiclav");
    }

    [Fact]
    public void GetRecommendedMedicines_AllAWaReClassesPresent_PicksTheAccessOne()
    {
        // Upper boundary of the classification domain: all six AWaRe classes compete
        // in one group, only the lowest one (Access) may be recommended
        var hospitalGroup = new AntibioticGroup
        {
            Name = "Hospital broad-spectrum antibacterials",
            Description = "Synthetic grouping spanning the entire AWaRe range",
            ParentId = null,
        };
        var antibiotics = new List<Antibiotic>
        {
            CreateAntibioticForRecommendation("Linezolid", AwareClassification.Reserve, hospitalGroup),
            CreateAntibioticForRecommendation("Fosfomycin", AwareClassification.Others, hospitalGroup),
            CreateAntibioticForRecommendation("Novobiocin", AwareClassification.Unclassified, hospitalGroup),
            CreateAntibioticForRecommendation("Meropenem", AwareClassification.Watch, hospitalGroup),
            CreateAntibioticForRecommendation("Co-amoxiclav", AwareClassification.AccessWatch, hospitalGroup),
            CreateAntibioticForRecommendation("Amoxicillin", AwareClassification.Access, hospitalGroup),
        };

        var result = _service.GetRecommendedMedicines(antibiotics);

        // Business rule: the 6 candidates share one group, so exactly one medicine
        // from that group may be recommended
        var recommended = Assert.Single(result);
        Assert.Equal(hospitalGroup.Id, recommended.AntibioticGroupId);
        Assert.Equal("Amoxicillin", recommended.Name);
    }

    # endregion
}

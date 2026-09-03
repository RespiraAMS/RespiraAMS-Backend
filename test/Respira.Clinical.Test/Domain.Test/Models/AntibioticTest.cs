using Domain.Models;
using Domain.Enums;
using Range = Domain.Models.Range;
using Respira.ServiceDefaults.Contracts.Results;

namespace Domain.Test.Models
{
    public class AntibioticTest
    {
        private static Dosage CreateDosage(RouteOfAdministration route, string dose = "500mg", Range? crcl = null)
        {
            return new Dosage
            {
                AntibioticId = Guid.CreateVersion7(),
                Dose = dose,
                RouteOfAdministration = route,
                Crcl = crcl
            };
        }

        private static Range CreateCrclRange(decimal min, decimal max, bool isMinExclusive = false, bool isMaxExclusive = false)
        {
            return new Range
            {
                Min = min,
                IsMinExclusive = isMinExclusive,
                Max = max,
                IsMaxExclusive = isMaxExclusive,
                Unit = "mL/min"
            };
        }

        #region Rule 1: Dosage list must not be empty

        public static readonly TheoryData<List<Dosage>> EmptyDosage =
        [
            new List<Dosage>()
        ];

        [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
        [MemberData(nameof(EmptyDosage))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
        public void IsAntibioticDosageValid_EmptyDosage_Fail(List<Dosage> dosages)
        {
            var result = Antibiotic.IsAntibioticDosageValid(dosages);
            Assert.Equal(Status.BusinessRuleViolation, result.StatusCode);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
        }

        #endregion

        #region Rule 2: Each route must have exactly 1 standard dose (CrCl == null)

        #region Rule 2 - Happy path

        public static readonly TheoryData<List<Dosage>> ValidStandardDoseCases =
        [
            // Single route, 1 standard dose only (no adjusted doses)
            new List<Dosage>
            {
                CreateDosage(RouteOfAdministration.Oral, "500mg")
            },

            // Single route, 1 standard + 1 adjusted dose
            new List<Dosage>
            {
                CreateDosage(RouteOfAdministration.Oral, "500mg"),
                CreateDosage(RouteOfAdministration.Oral, "250mg",
                    CreateCrclRange(15, 30, isMaxExclusive: true))
            },

            // Single route, 1 standard + multiple adjusted doses
            new List<Dosage>
            {
                CreateDosage(RouteOfAdministration.Oral, "500mg"),
                CreateDosage(RouteOfAdministration.Oral, "250mg",CreateCrclRange(15, 30, isMaxExclusive: true)),
                CreateDosage(RouteOfAdministration.Oral, "125mg",CreateCrclRange(30, 60, isMaxExclusive: true))
            },

            // Multiple routes, each with exactly 1 standard dose
            new List<Dosage>
            {
                CreateDosage(RouteOfAdministration.Oral, "500mg"),
                CreateDosage(RouteOfAdministration.Oral, "250mg", CreateCrclRange(15, 30, isMaxExclusive: true)),
                CreateDosage(RouteOfAdministration.Intravenous, "1g"),
                CreateDosage(RouteOfAdministration.Intravenous, "500mg", CreateCrclRange(15, 30, isMaxExclusive: true))
            }
        ];

        [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
        [MemberData(nameof(ValidStandardDoseCases))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
        public void IsAntibioticDosageValid_ValidStandardDose_Success(List<Dosage> dosages)
        {
            var result = Antibiotic.IsAntibioticDosageValid(dosages);
            Assert.True(result.IsSuccess());
            Assert.True(result.Data);
            Assert.Null(result.Error);
            Assert.Equal(Status.Success, result.StatusCode);
        }

        #endregion

        #region Rule 2 - Fail: No standard dose

        public static readonly TheoryData<List<Dosage>> NoStandardDose =
        [
            // Single route, all doses have CrCl (no standard dose)
            new List<Dosage>
            {
                CreateDosage(RouteOfAdministration.Oral, "250mg", CreateCrclRange(15, 30, isMaxExclusive: true)),
                CreateDosage(RouteOfAdministration.Oral, "125mg", CreateCrclRange(30, 60, isMaxExclusive: true))
            },

            // Multiple routes, one route has no standard dose
            new List<Dosage>
            {
                CreateDosage(RouteOfAdministration.Oral, "500mg"),
                CreateDosage(RouteOfAdministration.Intravenous, "1g", CreateCrclRange(15, 30, isMaxExclusive: true)),
                CreateDosage(RouteOfAdministration.Intravenous, "500mg", CreateCrclRange(30, 60, isMaxExclusive: true))
            }
        ];

        [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
        [MemberData(nameof(NoStandardDose))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
        public void IsAntibioticDosageValid_NoStandardDose_Fail(List<Dosage> dosages)
        {
            var result = Antibiotic.IsAntibioticDosageValid(dosages);
            Assert.Equal(Status.BusinessRuleViolation, result.StatusCode);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
        }

        #endregion

        #region Rule 2 - Fail: More than 1 standard dose

        public static readonly TheoryData<List<Dosage>> MoreThanOneStandardDose =
        [
            // Single route, 2 standard doses
            new List<Dosage>
            {
                CreateDosage(RouteOfAdministration.Oral, "500mg"),
                CreateDosage(RouteOfAdministration.Oral, "250mg")
            },

            // Single route, 3 standard doses
            new List<Dosage>
            {
                CreateDosage(RouteOfAdministration.Oral, "500mg"),
                CreateDosage(RouteOfAdministration.Oral, "250mg"),
                CreateDosage(RouteOfAdministration.Oral, "125mg")
            },

            // Multiple routes, one route has 2 standard doses
            new List<Dosage>
            {
                CreateDosage(RouteOfAdministration.Oral, "500mg"),
                CreateDosage(RouteOfAdministration.Oral, "250mg"),
                CreateDosage(RouteOfAdministration.Intravenous, "1g")
            },

            // Multiple routes, both routes have 2 standard doses
            new List<Dosage>
            {
                CreateDosage(RouteOfAdministration.Oral, "500mg"),
                CreateDosage(RouteOfAdministration.Oral, "250mg"),
                CreateDosage(RouteOfAdministration.Intravenous, "1g"),
                CreateDosage(RouteOfAdministration.Intravenous, "500mg")
            }
        ];

        [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
        [MemberData(nameof(MoreThanOneStandardDose))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
        public void IsAntibioticDosageValid_MoreThanOneStandardDose_ThrowsStandardDoseInvalidException(List<Dosage> dosages)
        {
            var result = Antibiotic.IsAntibioticDosageValid(dosages);
            Assert.Equal(Status.BusinessRuleViolation, result.StatusCode);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
        }

        #endregion

        #endregion

        #region Rule 3: CrCl ranges must not overlap per route

        #region Rule 3 - Happy path

        public static readonly TheoryData<List<Dosage>> NonOverlappingCrclRanges =
        [
            // Single route, standard + 2 non-overlapping adjusted doses
            // [15, 30) and [30, 60) - adjacent, no overlap (30 excluded from first)
            new List<Dosage>
            {
                CreateDosage(RouteOfAdministration.Oral, "500mg"),
                CreateDosage(RouteOfAdministration.Oral, "250mg", CreateCrclRange(15, 30, isMaxExclusive: true)),
                CreateDosage(RouteOfAdministration.Oral, "125mg", CreateCrclRange(30, 60, isMaxExclusive: true))
            },

            // Single route, standard + 3 non-overlapping adjusted doses
            // [15, 30), [30, 60), [60, 90) - chain of adjacent ranges
            new List<Dosage>
            {
                CreateDosage(RouteOfAdministration.Oral, "500mg"),
                CreateDosage(RouteOfAdministration.Oral, "250mg", CreateCrclRange(15, 30, isMaxExclusive: true)),
                CreateDosage(RouteOfAdministration.Oral, "125mg", CreateCrclRange(30, 60, isMaxExclusive: true)),
                CreateDosage(RouteOfAdministration.Oral, "62.5mg", CreateCrclRange(60, 90, isMaxExclusive: true))
            },

            // Single route, standard + 2 adjusted doses with a gap between them
            // [15, 30) and [60, 90) - gap between 30 and 60
            new List<Dosage>
            {
                CreateDosage(RouteOfAdministration.Oral, "500mg"),
                CreateDosage(RouteOfAdministration.Oral, "250mg", CreateCrclRange(15, 30, isMaxExclusive: true)),
                CreateDosage(RouteOfAdministration.Oral, "125mg", CreateCrclRange(60, 90, isMaxExclusive: true))
            },

            // Single route, standard + 2 adjusted doses that are open on both sides at boundary
            // (15, 30) and (30, 60) - no overlap at 30
            new List<Dosage>
            {
                CreateDosage(RouteOfAdministration.Oral, "500mg"),
                CreateDosage(RouteOfAdministration.Oral, "250mg", CreateCrclRange(15, 30, isMinExclusive: true, isMaxExclusive: true)),
                CreateDosage(RouteOfAdministration.Oral, "125mg", CreateCrclRange(30, 60, isMinExclusive: true, isMaxExclusive: true))
            },

            // Multiple routes, each with non-overlapping adjusted doses
            new List<Dosage>
            {
                CreateDosage(RouteOfAdministration.Oral, "500mg"),
                CreateDosage(RouteOfAdministration.Oral, "250mg", CreateCrclRange(15, 30, isMaxExclusive: true)),
                CreateDosage(RouteOfAdministration.Oral, "125mg", CreateCrclRange(30, 60, isMaxExclusive: true)),
                CreateDosage(RouteOfAdministration.Intravenous, "1g"),
                CreateDosage(RouteOfAdministration.Intravenous, "500mg", CreateCrclRange(15, 30, isMaxExclusive: true)),
                CreateDosage(RouteOfAdministration.Intravenous, "250mg", CreateCrclRange(30, 60, isMaxExclusive: true))
            },

            // Standard dose (CrCl null) is skipped in overlap check,
            // only adjusted doses are compared
            new List<Dosage>
            {
                CreateDosage(RouteOfAdministration.Oral, "500mg"),
                CreateDosage(RouteOfAdministration.Oral, "250mg", CreateCrclRange(15, 30, isMaxExclusive: true))
            }
        ];

        [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
        [MemberData(nameof(NonOverlappingCrclRanges))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
        public void IsAntibioticDosageValid_NonOverlappingCrcl_Success(List<Dosage> dosages)
        {
            var result = Antibiotic.IsAntibioticDosageValid(dosages);
            Assert.True(result.IsSuccess());
            Assert.True(result.Data);
            Assert.Null(result.Error);
            Assert.Equal(Status.Success, result.StatusCode);
        }

        #endregion

        #region Rule 3 - Fail: Overlapping CrCl ranges

        public static readonly TheoryData<List<Dosage>> OverlappingCrclRanges =
        [
            // Two adjusted doses with full overlap: [15, 60) and [30, 90)
            new List<Dosage>
            {
                CreateDosage(RouteOfAdministration.Oral, "500mg"),
                CreateDosage(RouteOfAdministration.Oral, "250mg", CreateCrclRange(15, 60, isMaxExclusive: true)),
                CreateDosage(RouteOfAdministration.Oral, "125mg", CreateCrclRange(30, 90, isMaxExclusive: true))
            },

            // Two adjusted doses, one contained in the other: [15, 90) contains [30, 60)
            new List<Dosage>
            {
                CreateDosage(RouteOfAdministration.Oral, "500mg"),
                CreateDosage(RouteOfAdministration.Oral, "250mg", CreateCrclRange(15, 90, isMaxExclusive: true)),
                CreateDosage(RouteOfAdministration.Oral, "125mg", CreateCrclRange(30, 60, isMaxExclusive: true))
            },

            // Two identical adjusted doses: [15, 30) and [15, 30)
            new List<Dosage>
            {
                CreateDosage(RouteOfAdministration.Oral, "500mg"),
                CreateDosage(RouteOfAdministration.Oral, "250mg", CreateCrclRange(15, 30, isMaxExclusive: true)),
                CreateDosage(RouteOfAdministration.Oral, "250mg", CreateCrclRange(15, 30, isMaxExclusive: true))
            },

            // Adjacent ranges that overlap at boundary: [15, 30] and [30, 60)
            // 30 is included in first range and included in second range -> overlap
            new List<Dosage>
            {
                CreateDosage(RouteOfAdministration.Oral, "500mg"),
                CreateDosage(RouteOfAdministration.Oral, "250mg", CreateCrclRange(15, 30, isMaxExclusive: false)),
                CreateDosage(RouteOfAdministration.Oral, "125mg", CreateCrclRange(30, 60, isMinExclusive: false, isMaxExclusive: true))
            },

            // Adjacent ranges that overlap at boundary: [15, 30) and [30, 60]
            // 30 is excluded from first but included in second -> no overlap
            // This should NOT throw, but included here to verify edge case
            // Actually this is valid, move to happy path? No - let's test the actual overlap case.
            // [15, 30] and [30, 60] -> both include 30 -> overlap
            new List<Dosage>
            {
                CreateDosage(RouteOfAdministration.Oral, "500mg"),
                CreateDosage(RouteOfAdministration.Oral, "250mg", CreateCrclRange(15, 30)),
                CreateDosage(RouteOfAdministration.Oral, "125mg", CreateCrclRange(30, 60))
            },

            // Multiple routes, one route has overlap
            new List<Dosage>
            {
                CreateDosage(RouteOfAdministration.Oral, "500mg"),
                CreateDosage(RouteOfAdministration.Oral, "250mg", CreateCrclRange(15, 60, isMaxExclusive: true)),
                CreateDosage(RouteOfAdministration.Oral, "125mg", CreateCrclRange(30, 90, isMaxExclusive: true)),
                CreateDosage(RouteOfAdministration.Intravenous, "1g"),
                CreateDosage(RouteOfAdministration.Intravenous, "500mg", CreateCrclRange(15, 30, isMaxExclusive: true)),
                CreateDosage(RouteOfAdministration.Intravenous, "250mg", CreateCrclRange(30, 60, isMaxExclusive: true))
            }
        ];

        [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
        [MemberData(nameof(OverlappingCrclRanges))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
        public void IsAntibioticDosageValid_OverlappingCrcl_Fail(List<Dosage> dosages)
        {
            var result = Antibiotic.IsAntibioticDosageValid(dosages);
            Assert.Equal(Status.BusinessRuleViolation, result.StatusCode);
            Assert.True(result.IsFailure());
            Assert.NotNull(result.Error);
        }

        #endregion

        #endregion
    }
}

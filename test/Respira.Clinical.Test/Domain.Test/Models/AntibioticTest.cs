using Domain.Exceptions;
using Domain.Models;
using Domain.Enums;

namespace Domain.Test.Models
{
    public class AntibioticTest
    {
        /*=== TEST RULE 1 ===*/

        public static readonly TheoryData<List<Dosage>> EmptyDosage =
        [
            new List<Dosage>()
        ];

        [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
        [MemberData(nameof(EmptyDosage))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
        public void IsAntibioticDosageValid_Rule1_Fail(List<Dosage> dosages)
        {
            Assert.Throws<DosageEmptyException>(() => Antibiotic.IsAntibioticDosageValid(dosages));
        }

        /*=== TEST RULE 2 ===*/

        public static readonly TheoryData<List<Dosage>> MoreThanOneStandardDose =
        [
            new List<Dosage>() {
            new() {
                AntibioticId = Guid.CreateVersion7(),
                Dose = "",
                RouteOfAdministration = RouteOfAdministration.Oral,
                Crcl = null
            },
            new() {
                AntibioticId = Guid.CreateVersion7(),
                Dose = "",
                RouteOfAdministration = RouteOfAdministration.Oral,
                Crcl = null
            },
        },
        new List<Dosage>() {
            new() {
                AntibioticId = Guid.CreateVersion7(),
                Dose = "",
                RouteOfAdministration = RouteOfAdministration.Oral,
                Crcl = null
            },
            new() {
                AntibioticId = Guid.CreateVersion7(),
                Dose = "",
                RouteOfAdministration = RouteOfAdministration.Intravenous,
                Crcl = null
            },
            new() {
                AntibioticId = Guid.CreateVersion7(),
                Dose = "",
                RouteOfAdministration = RouteOfAdministration.Intravenous,
                Crcl = null
            },
        }
        ];

        [Theory]
#pragma warning disable xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
        [MemberData(nameof(MoreThanOneStandardDose))]
#pragma warning restore xUnit1045 // Avoid using TheoryData type arguments that might not be serializable
        public void IsAntibioticDosageValid_Rule2_Fail(List<Dosage> dosages)
        {
            Assert.Throws<StandardDoseInvalidException>(() => Antibiotic.IsAntibioticDosageValid(dosages));
        }
    }
}

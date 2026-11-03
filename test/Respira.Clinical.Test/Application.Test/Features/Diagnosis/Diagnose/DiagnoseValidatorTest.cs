using Respira.Clinical.Application.Features.Diagnosis.Diagnose;
using Respira.Clinical.Domain.Enums;

namespace Respira.Clinical.Application.Test.Features.Diagnosis.Diagnose
{
    public class DiagnoseValidatorTest
    {
        private readonly DiagnoseValidator _validator = new();

        #region Valid query

        [Fact]
        public async Task Diagnose_MinimalValues_Success()
        {
            // Boundary: no observation, no sample, no other infection and no special
            // medical history is the smallest accepted diagnosis request
            var query = CreateQuery();

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task Diagnose_FullValues_Success()
        {
            var query = CreateQuery() with
            {
                Observations = [new ObservationQuery(Guid.CreateVersion7(), "38.7")],
                Allergy = new AllergyQuery
                {
                    Type = AllergyType.Yes,
                    AllergicAntibioticIds = [Guid.CreateVersion7()],
                    OtherAllergy = null,
                },
                SpecialMedicalHistory = ["Chronic kidney disease stage 3a"],
                OtherInfections = [InfectionType.Sepsis],
                MicrobialSamples =
                [
                    new MicrobialSampleQuery
                    {
                        SampleType = MicrobialSampleType.Sputum,
                        MicCategory = MicCategory.Susceptible,
                        PathogenId = Guid.CreateVersion7(),
                    },
                ],
            };

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // Each allergy type has one valid semantic combination
        [Theory]
        [InlineData(AllergyType.No)]
        [InlineData(AllergyType.Yes)]
        [InlineData(AllergyType.NonRegisteredAntibiotic)]
        [InlineData(AllergyType.Unknown)]
        public async Task Diagnose_ValidAllergyCombination_Success(AllergyType allergyType)
        {
            var query = CreateQuery() with
            {
                Allergy = allergyType switch
                {
                    AllergyType.No => new AllergyQuery
                    {
                        Type = allergyType,
                        AllergicAntibioticIds = [],
                        OtherAllergy = null,
                    },
                    AllergyType.Yes => new AllergyQuery
                    {
                        Type = allergyType,
                        AllergicAntibioticIds = [Guid.CreateVersion7()],
                        OtherAllergy = null,
                    },
                    _ => new AllergyQuery
                    {
                        Type = allergyType,
                        AllergicAntibioticIds = [],
                        OtherAllergy = "Penicillin G",
                    },
                },
            };

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // Boundary values of the sample enums: the lowest and highest defined members
        [Theory]
        [InlineData(MicrobialSampleType.Sputum, MicCategory.Susceptible)]
        [InlineData(MicrobialSampleType.Other, MicCategory.Resistant)]
        public async Task Diagnose_SampleEnumBoundaries_Success(
            MicrobialSampleType sampleType, MicCategory micCategory)
        {
            var query = CreateQuery() with
            {
                MicrobialSamples =
                [
                    new MicrobialSampleQuery
                    {
                        SampleType = sampleType,
                        MicCategory = micCategory,
                        PathogenId = Guid.CreateVersion7(),
                    },
                ],
            };

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        // Boundary values of InfectionType: 1 is the lowest member, 31 the highest
        // contiguous member and 99 the last member
        [Theory]
        [InlineData(InfectionType.SkinAndSoftTissueInfection)]
        [InlineData(InfectionType.MaxillofacialAbscess)]
        [InlineData(InfectionType.Other)]
        public async Task Diagnose_OtherInfectionBoundaries_Success(InfectionType infectionType)
        {
            var query = CreateQuery() with
            {
                OtherInfections = [infectionType],
            };

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            Assert.Empty(result.Errors);
        }

        #endregion

        #region Invalid query

        [Fact]
        public async Task Diagnose_ObservationWithEmptyVariableId_Fail()
        {
            // Boundary: Guid.Empty is the only UUID value rejected by the rule
            var query = CreateQuery() with
            {
                Observations = [new ObservationQuery(Guid.Empty, "38.7")],
            };

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Observations[0].VariableId", result.Errors[0].PropertyName);
        }

        // Boundary: empty and whitespace are the only strings rejected by NotEmpty
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Diagnose_ObservationWithEmptyValue_Fail(string value)
        {
            var query = CreateQuery() with
            {
                Observations = [new ObservationQuery(Guid.CreateVersion7(), value)],
            };

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Observations[0].Value", result.Errors[0].PropertyName);
        }

        // AllergyType members are 0, 1, 2 and 3, so -1 and 4 are the adjacent invalid values
        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        public async Task Diagnose_InvalidAllergyType_Fail(int allergyType)
        {
            var query = CreateQuery() with
            {
                Allergy = new AllergyQuery
                {
                    Type = (AllergyType)allergyType,
                    AllergicAntibioticIds = [],
                    // Keep the semantic rule satisfied so only the enum rule fails
                    OtherAllergy = "Penicillin G",
                },
            };

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Allergy.Type", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task Diagnose_AllergicAntibioticWithEmptyId_Fail()
        {
            var query = CreateQuery() with
            {
                Allergy = new AllergyQuery
                {
                    Type = AllergyType.Yes,
                    AllergicAntibioticIds = [Guid.Empty],
                    OtherAllergy = null,
                },
            };

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Allergy.AllergicAntibioticIds[0]", result.Errors[0].PropertyName);
        }

        // Boundary: an empty string and a whitespace-only string are both rejected
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Diagnose_OtherAllergyEmpty_Fail(string otherAllergy)
        {
            var query = CreateQuery() with
            {
                Allergy = new AllergyQuery
                {
                    Type = AllergyType.NonRegisteredAntibiotic,
                    AllergicAntibioticIds = [],
                    OtherAllergy = otherAllergy,
                },
            };

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Allergy.OtherAllergy", result.Errors[0].PropertyName);
        }

        /*
         * Semantically conflicted allergy combinations:
         * - No          requires no IDs and no other allergy
         * - Yes         requires at least 1 ID and no other allergy
         * - NonRegisteredAntibiotic / Unknown require no IDs and an other allergy text
         */
        [Theory]
        [InlineData(AllergyType.No, true, false)]
        [InlineData(AllergyType.No, false, true)]
        [InlineData(AllergyType.No, true, true)]
        [InlineData(AllergyType.Yes, false, false)]
        [InlineData(AllergyType.Yes, true, true)]
        [InlineData(AllergyType.NonRegisteredAntibiotic, true, true)]
        [InlineData(AllergyType.NonRegisteredAntibiotic, false, false)]
        [InlineData(AllergyType.Unknown, true, true)]
        [InlineData(AllergyType.Unknown, false, false)]
        public async Task Diagnose_ConflictedAllergy_Fail(
            AllergyType allergyType, bool hasAllergicAntibiotic, bool hasOtherAllergy)
        {
            var query = CreateQuery() with
            {
                Allergy = new AllergyQuery
                {
                    Type = allergyType,
                    AllergicAntibioticIds = hasAllergicAntibiotic
                        ? [Guid.CreateVersion7()]
                        : [],
                    OtherAllergy = hasOtherAllergy ? "Penicillin G" : null,
                },
            };

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("Allergy", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task Diagnose_MicrobialSampleWithEmptyPathogenId_Fail()
        {
            var query = CreateQuery() with
            {
                MicrobialSamples =
                [
                    new MicrobialSampleQuery
                    {
                        SampleType = MicrobialSampleType.Sputum,
                        MicCategory = MicCategory.Susceptible,
                        PathogenId = Guid.Empty,
                    },
                ],
            };

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("MicrobialSamples[0].PathogenId", result.Errors[0].PropertyName);
        }

        // MicCategory members are 0, 1 and 2, so -1 and 3 are the adjacent invalid values
        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        public async Task Diagnose_InvalidMicCategory_Fail(int micCategory)
        {
            var query = CreateQuery() with
            {
                MicrobialSamples =
                [
                    new MicrobialSampleQuery
                    {
                        SampleType = MicrobialSampleType.Sputum,
                        MicCategory = (MicCategory)micCategory,
                        PathogenId = Guid.CreateVersion7(),
                    },
                ],
            };

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("MicrobialSamples[0].MicCategory", result.Errors[0].PropertyName);
        }

        // MicrobialSampleType members are 0..8, so -1 and 9 are the adjacent invalid values
        [Theory]
        [InlineData(-1)]
        [InlineData(9)]
        public async Task Diagnose_InvalidSampleType_Fail(int sampleType)
        {
            var query = CreateQuery() with
            {
                MicrobialSamples =
                [
                    new MicrobialSampleQuery
                    {
                        SampleType = (MicrobialSampleType)sampleType,
                        MicCategory = MicCategory.Susceptible,
                        PathogenId = Guid.CreateVersion7(),
                    },
                ],
            };

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("MicrobialSamples[0].SampleType", result.Errors[0].PropertyName);
        }

        /*
         * InfectionType has members 1..31 and 99. The invalid boundaries are 0 (below
         * the lowest member), 32 (first invalid after the contiguous block), 98 (last
         * invalid before Other) and 100 (above the highest member)
         */
        [Theory]
        [InlineData(0)]
        [InlineData(32)]
        [InlineData(98)]
        [InlineData(100)]
        public async Task Diagnose_InvalidOtherInfection_Fail(int infectionType)
        {
            var query = CreateQuery() with
            {
                OtherInfections = [(InfectionType)infectionType],
            };

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("OtherInfections[0]", result.Errors[0].PropertyName);
        }

        // Boundary: empty and whitespace are the only strings rejected by NotEmpty
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Diagnose_EmptySpecialMedicalHistory_Fail(string history)
        {
            var query = CreateQuery() with
            {
                SpecialMedicalHistory = [history],
            };

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            _ = Assert.Single(result.Errors);
            Assert.Equal("SpecialMedicalHistory[0]", result.Errors[0].PropertyName);
        }

        [Fact]
        public async Task Diagnose_AllSectionsInvalid_Fail()
        {
            var query = new DiagnoseQuery
            {
                Observations = [new ObservationQuery(Guid.Empty, "")],
                Allergy = new AllergyQuery
                {
                    Type = (AllergyType)4,
                    AllergicAntibioticIds = [Guid.Empty],
                    OtherAllergy = " ",
                },
                SpecialMedicalHistory = [" "],
                OtherInfections = [0],
                MicrobialSamples =
                [
                    new MicrobialSampleQuery
                    {
                        SampleType = (MicrobialSampleType)9,
                        MicCategory = (MicCategory)3,
                        PathogenId = Guid.Empty,
                    },
                ],
            };

            var result = await _validator.ValidateAsync(
                query, TestContext.Current.CancellationToken);

            Assert.Contains(result.Errors, x => x.PropertyName == "Observations[0].VariableId");
            Assert.Contains(result.Errors, x => x.PropertyName == "Observations[0].Value");
            Assert.Contains(result.Errors, x => x.PropertyName == "Allergy.Type");
            Assert.Contains(result.Errors, x => x.PropertyName == "Allergy.AllergicAntibioticIds[0]");
            Assert.Contains(result.Errors, x => x.PropertyName == "Allergy.OtherAllergy");
            Assert.Contains(result.Errors, x => x.PropertyName == "Allergy");
            Assert.Contains(result.Errors, x => x.PropertyName == "SpecialMedicalHistory[0]");
            Assert.Contains(result.Errors, x => x.PropertyName == "OtherInfections[0]");
            Assert.Contains(result.Errors, x => x.PropertyName == "MicrobialSamples[0].PathogenId");
            Assert.Contains(result.Errors, x => x.PropertyName == "MicrobialSamples[0].MicCategory");
            Assert.Contains(result.Errors, x => x.PropertyName == "MicrobialSamples[0].SampleType");
        }

        #endregion

        private static DiagnoseQuery CreateQuery()
        {
            return new DiagnoseQuery
            {
                Observations = [],
                Allergy = new AllergyQuery
                {
                    Type = AllergyType.No,
                    AllergicAntibioticIds = [],
                    OtherAllergy = null,
                },
                SpecialMedicalHistory = [],
                OtherInfections = [],
                MicrobialSamples = [],
            };
        }
    }
}

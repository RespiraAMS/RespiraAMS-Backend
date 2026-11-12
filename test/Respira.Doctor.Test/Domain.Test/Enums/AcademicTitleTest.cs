using System.Text.Json;
using System.Text.Json.Serialization;
using Respira.Doctor.Domain.Enums;

namespace Respira.Doctor.Domain.Test.Enums
{
    public class AcademicTitleTest
    {
        [Fact]
        public void AcademicTitle_HasThreeDeclaredMembersWithExpectedOrdinals()
        {
            var values = Enum.GetValues<AcademicTitle>();

            Assert.Equal(3, values.Length);
            Assert.Equal(0, (int)AcademicTitle.None);
            Assert.Equal(1, (int)AcademicTitle.AssociateProfessor);
            Assert.Equal(2, (int)AcademicTitle.Professor);
        }

        [Theory]
        [InlineData(AcademicTitle.None, "None")]
        [InlineData(AcademicTitle.AssociateProfessor, "AssociateProfessor")]
        [InlineData(AcademicTitle.Professor, "Professor")]
        public void ParseAndSerialize_RoundTripEveryMember(
            AcademicTitle title,
            string expectedName
        )
        {
            Assert.Equal(expectedName, title.ToString());
            Assert.Equal(title, Enum.Parse<AcademicTitle>(expectedName));
            Assert.True(Enum.TryParse<AcademicTitle>(expectedName, out var parsed));
            Assert.Equal(title, parsed);
            Assert.True(Enum.IsDefined(title));

            var json = JsonSerializer.Serialize(title);
            Assert.Equal($"\"{expectedName}\"", json);
            Assert.Equal(title, JsonSerializer.Deserialize<AcademicTitle>(json));
        }

        [Fact]
        public void AcademicTitle_IsAnnotatedWithJsonStringEnumConverter()
        {
            var attribute = (JsonConverterAttribute)
                typeof(AcademicTitle)
                    .GetCustomAttributes(typeof(JsonConverterAttribute), inherit: false)
                    .Single();

            Assert.Equal(typeof(JsonStringEnumConverter), attribute.ConverterType);
        }

        [Fact]
        public void IsDefined_RejectsUndefinedOrdinal()
        {
            Assert.False(Enum.IsDefined((AcademicTitle)99));
            Assert.False(Enum.IsDefined((AcademicTitle)(-1)));
        }
    }
}

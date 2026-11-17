using System.Text.Json;
using System.Text.Json.Serialization;
using Respira.Doctor.Domain.Enums;

namespace Respira.Doctor.Domain.Test.Enums
{
    public class DegreeTypeTest
    {
        [Theory]
        [InlineData(DegreeType.Professor, "Professor", 0)]
        [InlineData(DegreeType.Doctor, "Doctor", 1)]
        [InlineData(DegreeType.Master, "Master", 2)]
        [InlineData(DegreeType.Bachelor, "Bachelor", 3)]
        [InlineData(DegreeType.Associate, "Associate", 4)]
        [InlineData(DegreeType.Undergraduate, "Undergraduate", 5)]
        [InlineData(DegreeType.Graduate, "Graduate", 6)]
        [InlineData(DegreeType.PhD, "PhD", 7)]
        [InlineData(DegreeType.PostDoc, "PostDoc", 8)]
        public void ParseAndSerialize_RoundTripEveryMember(
            DegreeType degree,
            string expectedName,
            int expectedOrdinal
        )
        {
            Assert.Equal(expectedOrdinal, (int)degree);
            Assert.Equal(expectedName, degree.ToString());
            Assert.Equal(degree, Enum.Parse<DegreeType>(expectedName));
            Assert.True(Enum.TryParse<DegreeType>(expectedName, out var parsed));
            Assert.Equal(degree, parsed);
            Assert.True(Enum.IsDefined(degree));

            var json = JsonSerializer.Serialize(degree);
            Assert.Equal($"\"{expectedName}\"", json);
            Assert.Equal(degree, JsonSerializer.Deserialize<DegreeType>(json));
        }

        [Fact]
        public void DegreeType_IsAnnotatedWithJsonStringEnumConverter()
        {
            var attribute = (JsonConverterAttribute)
                typeof(DegreeType)
                    .GetCustomAttributes(typeof(JsonConverterAttribute), inherit: false)
                    .Single();

            Assert.Equal(typeof(JsonStringEnumConverter), attribute.ConverterType);
        }

        [Fact]
        public void IsDefined_RejectsUndefinedOrdinal()
        {
            Assert.False(Enum.IsDefined((DegreeType)99));
            Assert.False(Enum.IsDefined((DegreeType)(-1)));
        }
    }
}

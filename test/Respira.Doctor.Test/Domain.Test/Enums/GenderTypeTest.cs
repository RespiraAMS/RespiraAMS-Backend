using System.Text.Json;
using System.Text.Json.Serialization;
using Respira.Doctor.Domain.Enums;

namespace Respira.Doctor.Domain.Test.Enums
{
    public class GenderTypeTest
    {
        [Theory]
        [InlineData(GenderType.Male, "Male", 0)]
        [InlineData(GenderType.Female, "Female", 1)]
        public void ParseAndSerialize_RoundTripEveryMember(
            GenderType gender,
            string expectedName,
            int expectedOrdinal
        )
        {
            Assert.Equal(expectedOrdinal, (int)gender);
            Assert.Equal(expectedName, gender.ToString());
            Assert.Equal(gender, Enum.Parse<GenderType>(expectedName));
            Assert.True(Enum.TryParse<GenderType>(expectedName, out var parsed));
            Assert.Equal(gender, parsed);
            Assert.True(Enum.IsDefined(gender));

            var json = JsonSerializer.Serialize(gender);
            Assert.Equal($"\"{expectedName}\"", json);
            Assert.Equal(gender, JsonSerializer.Deserialize<GenderType>(json));
        }

        [Fact]
        public void GenderType_IsAnnotatedWithJsonStringEnumConverter()
        {
            var attribute = (JsonConverterAttribute)
                typeof(GenderType)
                    .GetCustomAttributes(typeof(JsonConverterAttribute), inherit: false)
                    .Single();

            Assert.Equal(typeof(JsonStringEnumConverter), attribute.ConverterType);
        }

        [Fact]
        public void IsDefined_RejectsUndefinedOrdinal()
        {
            Assert.False(Enum.IsDefined((GenderType)99));
            Assert.False(Enum.IsDefined((GenderType)(-1)));
        }
    }
}

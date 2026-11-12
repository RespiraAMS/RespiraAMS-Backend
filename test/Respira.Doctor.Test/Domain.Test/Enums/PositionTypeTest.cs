using System.Text.Json;
using System.Text.Json.Serialization;
using Respira.Doctor.Domain.Enums;

namespace Respira.Doctor.Domain.Test.Enums
{
    public class PositionTypeTest
    {
        [Theory]
        [InlineData(PositionType.StaffDoctor, "StaffDoctor", 1)]
        [InlineData(PositionType.SeniorDoctor, "SeniorDoctor", 2)]
        [InlineData(PositionType.DepartmentDeputyHead, "DepartmentDeputyHead", 3)]
        [InlineData(PositionType.HeadOfDepartment, "HeadOfDepartment", 4)]
        [InlineData(PositionType.DeputyDirector, "DeputyDirector", 5)]
        [InlineData(PositionType.Director, "Director", 6)]
        public void ParseAndSerialize_RoundTripEveryMember(
            PositionType position,
            string expectedName,
            int expectedOrdinal
        )
        {
            Assert.Equal(expectedOrdinal, (int)position);
            Assert.Equal(expectedName, position.ToString());
            Assert.Equal(position, Enum.Parse<PositionType>(expectedName));
            Assert.True(Enum.TryParse<PositionType>(expectedName, out var parsed));
            Assert.Equal(position, parsed);
            Assert.True(Enum.IsDefined(position));

            var json = JsonSerializer.Serialize(position);
            Assert.Equal($"\"{expectedName}\"", json);
            Assert.Equal(position, JsonSerializer.Deserialize<PositionType>(json));
        }

        [Fact]
        public void PositionType_IsAnnotatedWithJsonStringEnumConverter()
        {
            var attribute = (JsonConverterAttribute)
                typeof(PositionType)
                    .GetCustomAttributes(typeof(JsonConverterAttribute), inherit: false)
                    .Single();

            Assert.Equal(typeof(JsonStringEnumConverter), attribute.ConverterType);
        }

        [Fact]
        public void IsDefined_RejectsZeroAndUndefinedOrdinals()
        {
            Assert.False(Enum.IsDefined((PositionType)0));
            Assert.False(Enum.IsDefined((PositionType)99));
            Assert.False(Enum.IsDefined((PositionType)(-1)));
        }
    }
}

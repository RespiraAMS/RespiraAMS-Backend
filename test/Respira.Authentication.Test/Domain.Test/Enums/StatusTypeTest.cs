using System.Text.Json;
using System.Text.Json.Serialization;
using Respira.Authentication.Domain.Enums;

namespace Respira.Authentication.Domain.Test.Enums
{
    public class StatusTypeTest
    {
        [Fact]
        public void StatusType_HasActiveAndInactiveWithStableOrdinals()
        {
            var values = Enum.GetValues<StatusType>();

            Assert.Equal(2, values.Length);
            Assert.Equal(0, (int)StatusType.Active);
            Assert.Equal(1, (int)StatusType.Inactive);
        }

        [Theory]
        [InlineData(StatusType.Active, "Active")]
        [InlineData(StatusType.Inactive, "Inactive")]
        public void ParseAndSerialize_RoundTripEveryMember(StatusType status, string expectedName)
        {
            Assert.Equal(expectedName, status.ToString());
            Assert.Equal(status, Enum.Parse<StatusType>(expectedName));
            Assert.True(Enum.TryParse<StatusType>(expectedName, out var parsed));
            Assert.Equal(status, parsed);
            Assert.True(Enum.IsDefined(status));

            var json = JsonSerializer.Serialize(status);
            Assert.Equal($"\"{expectedName}\"", json);
            Assert.Equal(status, JsonSerializer.Deserialize<StatusType>(json));
        }

        [Fact]
        public void StatusType_IsAnnotatedWithJsonStringEnumConverter()
        {
            var attribute = (JsonConverterAttribute)
                typeof(StatusType)
                    .GetCustomAttributes(typeof(JsonConverterAttribute), inherit: false)
                    .Single();

            Assert.Equal(typeof(JsonStringEnumConverter), attribute.ConverterType);
        }

        [Fact]
        public void IsDefined_RejectsUndefinedOrdinal()
        {
            Assert.False(Enum.IsDefined((StatusType)42));
            Assert.False(Enum.IsDefined((StatusType)(-1)));
        }
    }
}

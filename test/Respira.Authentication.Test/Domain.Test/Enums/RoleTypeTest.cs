using System.Text.Json;
using System.Text.Json.Serialization;
using Respira.Authentication.Domain.Enums;

namespace Respira.Authentication.Domain.Test.Enums
{
    public class RoleTypeTest
    {
        [Fact]
        public void RoleType_HasFourDeclaredMembersWithStableOrdinals()
        {
            var values = Enum.GetValues<RoleType>();

            Assert.Equal(4, values.Length);
            Assert.Equal(0, (int)RoleType.Doctor);
            Assert.Equal(1, (int)RoleType.MedicalRegulationReviewer);
            Assert.Equal(2, (int)RoleType.Admin);
            Assert.Equal(3, (int)RoleType.HeadDepartmentDoctor);
        }

        [Theory]
        [InlineData(RoleType.Doctor, "Doctor")]
        [InlineData(RoleType.MedicalRegulationReviewer, "MedicalRegulationReviewer")]
        [InlineData(RoleType.Admin, "Admin")]
        [InlineData(RoleType.HeadDepartmentDoctor, "HeadDepartmentDoctor")]
        public void ParseAndSerialize_RoundTripEveryMember(RoleType role, string expectedName)
        {
            Assert.Equal(expectedName, role.ToString());
            Assert.Equal(role, Enum.Parse<RoleType>(expectedName));
            Assert.True(Enum.TryParse<RoleType>(expectedName, out var parsed));
            Assert.Equal(role, parsed);
            Assert.True(Enum.IsDefined(role));

            var json = JsonSerializer.Serialize(role);
            Assert.Equal($"\"{expectedName}\"", json);
            Assert.Equal(role, JsonSerializer.Deserialize<RoleType>(json));
        }

        [Fact]
        public void RoleType_IsAnnotatedWithJsonStringEnumConverter()
        {
            var attribute = (JsonConverterAttribute)
                typeof(RoleType)
                    .GetCustomAttributes(typeof(JsonConverterAttribute), inherit: false)
                    .Single();

            Assert.Equal(typeof(JsonStringEnumConverter), attribute.ConverterType);
        }

        [Fact]
        public void IsDefined_RejectsUndefinedOrdinal()
        {
            Assert.False(Enum.IsDefined((RoleType)99));
            Assert.False(Enum.IsDefined((RoleType)(-1)));
        }
    }
}

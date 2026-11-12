using System.Text.Json;
using Respira.Doctor.Domain.Enums;

namespace Respira.Doctor.Domain.Test.Enums
{
    public class ProfileRoleTest
    {
        [Fact]
        public void ProfileRole_HasExactlyTwoMembers()
        {
            Assert.Equal(2, Enum.GetValues<ProfileRole>().Length);
        }

        [Fact]
        public void ProfileRole_DoctorHasOrdinalZero()
        {
            Assert.Equal(0, (int)ProfileRole.Doctor);
        }

        [Fact]
        public void ProfileRole_HeadDepartmentDoctorHasOrdinalOne()
        {
            Assert.Equal(1, (int)ProfileRole.HeadDepartmentDoctor);
        }

        [Theory]
        [InlineData(ProfileRole.Doctor)]
        [InlineData(ProfileRole.HeadDepartmentDoctor)]
        public void Parse_RoundTripsEveryMember(ProfileRole role)
        {
            var parsed = Enum.Parse<ProfileRole>(role.ToString());

            Assert.Equal(role, parsed);
        }

        [Theory]
        [InlineData(ProfileRole.Doctor)]
        [InlineData(ProfileRole.HeadDepartmentDoctor)]
        public void TryParse_RoundTripsEveryMember(ProfileRole role)
        {
            var parsed = Enum.TryParse<ProfileRole>(role.ToString(), out var result);

            Assert.True(parsed);
            Assert.Equal(role, result);
        }

        [Fact]
        public void GetValues_ReturnsExactlyTheDeclaredMembers()
        {
            var values = Enum.GetValues<ProfileRole>().ToArray();

            Assert.Equal(2, values.Length);
            Assert.Equal(ProfileRole.Doctor, values[0]);
            Assert.Equal(ProfileRole.HeadDepartmentDoctor, values[1]);
        }

        [Theory]
        [InlineData(ProfileRole.Doctor)]
        [InlineData(ProfileRole.HeadDepartmentDoctor)]
        public void IsDefined_AcceptsEveryDeclaredMember(ProfileRole role)
        {
            Assert.True(Enum.IsDefined(role));
        }

        [Fact]
        public void IsDefined_RejectsAnUndefinedMember()
        {
            Assert.False(Enum.IsDefined((ProfileRole)42));
            Assert.False(Enum.IsDefined((ProfileRole)(-1)));
        }

        [Theory]
        [InlineData(ProfileRole.Doctor, "Doctor")]
        [InlineData(ProfileRole.HeadDepartmentDoctor, "HeadDepartmentDoctor")]
        public void ToString_ReturnsTheMemberName(ProfileRole role, string expected)
        {
            Assert.Equal(expected, role.ToString());
            Assert.Equal(expected, Enum.GetName(role));
        }

        [Fact]
        public void Serialize_WritesTheJsonStringOfTheDoctorMember()
        {
            var json = JsonSerializer.Serialize(ProfileRole.Doctor);

            Assert.Equal("\"Doctor\"", json);
        }

        [Fact]
        public void Serialize_WritesTheJsonStringOfTheHeadDepartmentDoctorMember()
        {
            var json = JsonSerializer.Serialize(ProfileRole.HeadDepartmentDoctor);

            Assert.Equal("\"HeadDepartmentDoctor\"", json);
        }

        [Fact]
        public void Deserialize_ReadsBackTheJsonStringOfEveryMember()
        {
            Assert.Equal(ProfileRole.Doctor, JsonSerializer.Deserialize<ProfileRole>("\"Doctor\""));
            Assert.Equal(
                ProfileRole.HeadDepartmentDoctor,
                JsonSerializer.Deserialize<ProfileRole>("\"HeadDepartmentDoctor\"")
            );
        }
    }
}

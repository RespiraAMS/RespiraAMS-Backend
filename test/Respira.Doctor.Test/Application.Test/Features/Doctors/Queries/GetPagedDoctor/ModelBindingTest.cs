using System.ComponentModel;
using Respira.Doctor.Domain.Enums;

namespace Respira.Doctor.Application.Test.Features.Doctors.Queries.GetPagedDoctor;

public class ModelBindingTest
{
    [Theory]
    [InlineData("HeadDepartmentDoctor", ProfileRole.HeadDepartmentDoctor)]
    [InlineData("headdepartmentdoctor", ProfileRole.HeadDepartmentDoctor)]
    [InlineData("Doctor", ProfileRole.Doctor)]
    [InlineData("doctor", ProfileRole.Doctor)]
    [InlineData("headdoctor", ProfileRole.HeadDepartmentDoctor)]
    public void TypeDescriptor_ConvertsExpectedRoles(string input, ProfileRole expected)
    {
        var converter = TypeDescriptor.GetConverter(typeof(ProfileRole));
        var converted = converter.ConvertFrom(input);
        Assert.Equal(expected, converted);
    }
}

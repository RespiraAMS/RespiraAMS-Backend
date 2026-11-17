using Respira.Authentication.Domain.Enums;
using Respira.Saga.Application.Features.Create.Commands;

namespace Respira.Saga.Application.Test.Features.Create;

public sealed class ProfileCreationPolicyTest
{
    [Theory]
    [InlineData(RoleType.Admin, RoleType.Doctor, true)]
    [InlineData(RoleType.Admin, RoleType.MedicalRegulationReviewer, true)]
    [InlineData(RoleType.Admin, RoleType.HeadDepartmentDoctor, true)]
    [InlineData(RoleType.Admin, RoleType.Admin, false)]
    [InlineData(RoleType.HeadDepartmentDoctor, RoleType.Doctor, true)]
    [InlineData(RoleType.HeadDepartmentDoctor, RoleType.MedicalRegulationReviewer, false)]
    [InlineData(RoleType.HeadDepartmentDoctor, RoleType.HeadDepartmentDoctor, false)]
    [InlineData(RoleType.Doctor, RoleType.Doctor, false)]
    [InlineData(RoleType.MedicalRegulationReviewer, RoleType.Doctor, false)]
    public void CanCreate_ReturnsExpectedPermission(RoleType actor, RoleType target, bool expected)
    {
        Assert.Equal(expected, ProfileCreationPolicy.CanCreate(actor, target));
    }
}

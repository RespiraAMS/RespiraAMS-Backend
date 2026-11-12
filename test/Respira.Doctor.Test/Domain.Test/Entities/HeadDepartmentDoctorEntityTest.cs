using Respira.Doctor.Domain.Entities;
using Respira.Doctor.Domain.Enums;
using Respira.ServiceDefaults.Models;
using DoctorEntity = Respira.Doctor.Domain.Entities.Doctor;
using HeadDepartmentDoctorEntity = Respira.Doctor.Domain.Entities.HeadDepartmentDoctor;

namespace Respira.Doctor.Domain.Test.Entities
{
    public class HeadDepartmentDoctorEntityTest
    {
        private static HeadDepartmentDoctorEntity CreateHeadDepartmentDoctor() =>
            new()
            {
                FirstName = "Le",
                LastName = "Minh C",
                Degrees = [DegreeType.Doctor, DegreeType.PhD],
                AcademicTitle = AcademicTitle.Professor,
                Position = PositionType.HeadOfDepartment,
                Gender = GenderType.Male,
                CitizenIdentificationNumber = "001075011223",
                Address = "100 Hai Ba Trung, District 1, Ho Chi Minh City",
                AccountId = Guid.CreateVersion7(),
            };

        [Fact]
        public void NewHeadDepartmentDoctor_DerivesFromBaseDoctorAndFullTextSearch()
        {
            var head = CreateHeadDepartmentDoctor();

            Assert.IsAssignableFrom<BaseDoctor>(head);
            Assert.IsAssignableFrom<FullTextSearch>(head);
            Assert.IsAssignableFrom<Base>(head);
        }

        [Fact]
        public void Subordinates_CanBePopulatedWithManagedDoctors()
        {
            var head = CreateHeadDepartmentDoctor();
            var subordinate = new DoctorEntity
            {
                FirstName = "Pham",
                LastName = "Thi D",
                Degrees = [DegreeType.Doctor],
                AcademicTitle = AcademicTitle.None,
                Position = PositionType.StaffDoctor,
                Gender = GenderType.Female,
                CitizenIdentificationNumber = "001095099887",
                Address = "200 Dien Bien Phu, District 3, Ho Chi Minh City",
                HeadDepartmentDoctorId = head.Id,
            };

            head.Subordinates.Add(subordinate);

            var single = Assert.Single(head.Subordinates);
            Assert.Same(subordinate, single);
            Assert.Equal(head.Id, single.HeadDepartmentDoctorId);
        }

        [Fact]
        public void HeadDepartmentDoctor_CanManagePatientsListAndProfileFields()
        {
            var head = CreateHeadDepartmentDoctor();
            var patient1 = Guid.CreateVersion7();
            var patient2 = Guid.CreateVersion7();
            var dob = new DateTimeOffset(1975, 5, 10, 0, 0, 0, TimeSpan.Zero);
            var mediaId = Guid.CreateVersion7();

            head.Patients = [patient1, patient2];
            head.DateOfBirth = dob;
            head.MediaId = mediaId;

            Assert.Equal(2, head.Patients.Count);
            Assert.Contains(patient1, head.Patients);
            Assert.Contains(patient2, head.Patients);
            Assert.Equal(dob, head.DateOfBirth);
            Assert.Equal(mediaId, head.MediaId);
        }
    }
}

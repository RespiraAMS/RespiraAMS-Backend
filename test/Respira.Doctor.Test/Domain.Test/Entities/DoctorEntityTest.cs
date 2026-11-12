using Respira.Doctor.Domain.Entities;
using Respira.Doctor.Domain.Enums;
using DoctorEntity = Respira.Doctor.Domain.Entities.Doctor;
using HeadDepartmentDoctorEntity = Respira.Doctor.Domain.Entities.HeadDepartmentDoctor;

namespace Respira.Doctor.Domain.Test.Entities
{
    public class DoctorEntityTest
    {
        private static DoctorEntity CreateDoctor() =>
            new()
            {
                FirstName = "Nguyen",
                LastName = "Van A",
                Degrees = [DegreeType.Doctor, DegreeType.Master],
                AcademicTitle = AcademicTitle.None,
                Position = PositionType.StaffDoctor,
                Gender = GenderType.Male,
                CitizenIdentificationNumber = "001088012345",
                Address = "12 Le Loi, District 1, Ho Chi Minh City",
            };

        private static HeadDepartmentDoctorEntity CreateHeadDepartmentDoctor() =>
            new()
            {
                FirstName = "Tran",
                LastName = "Thi B",
                Degrees = [DegreeType.Doctor, DegreeType.PhD],
                AcademicTitle = AcademicTitle.Professor,
                Position = PositionType.HeadOfDepartment,
                Gender = GenderType.Female,
                CitizenIdentificationNumber = "001088054321",
                Address = "45 Le Loi, District 1, Ho Chi Minh City",
            };

        #region Identity and audit fields

        [Fact]
        public void NewDoctor_HasANonEmptyUuidV7Identifier()
        {
            var doctor = CreateDoctor();

            Assert.NotEqual(Guid.Empty, doctor.Id);
            // RFC 9562 UUID v7 keeps the version in the first nibble of the third group.
            Assert.Equal('7', doctor.Id.ToString()[14]);
        }

        [Fact]
        public void NewDoctor_HasCreatedAtAndUpdatedAtCloseToNow()
        {
            var before = DateTimeOffset.UtcNow;

            var doctor = CreateDoctor();

            var after = DateTimeOffset.UtcNow;
            Assert.InRange(doctor.CreatedAt, before, after);
            Assert.InRange(doctor.UpdatedAt, before, after);
            Assert.Equal(TimeSpan.Zero, doctor.CreatedAt.Offset);
            Assert.Equal(TimeSpan.Zero, doctor.UpdatedAt.Offset);
        }

        [Fact]
        public void NewDoctor_IsNotSoftDeletedAndHasNoDeletedAt()
        {
            var doctor = CreateDoctor();

            Assert.False(doctor.IsDeleted);
            Assert.Null(doctor.DeletedAt);
        }

        #endregion

        #region Relationships

        [Fact]
        public void NewHeadDepartmentDoctor_HasAnEmptySubordinatesCollection()
        {
            var head = CreateHeadDepartmentDoctor();

            Assert.NotNull(head.Subordinates);
            Assert.Empty(head.Subordinates);
        }

        [Fact]
        public void NewDoctor_HasNoHeadDepartmentDoctorAssigned()
        {
            var doctor = CreateDoctor();

            Assert.Null(doctor.HeadDepartmentDoctorId);
        }

        [Fact]
        public void SettingHeadDepartmentDoctorId_LinksTheDoctorToItsHead()
        {
            var doctor = CreateDoctor();
            var headId = Guid.CreateVersion7();

            doctor.HeadDepartmentDoctorId = headId;

            Assert.Equal(headId, doctor.HeadDepartmentDoctorId);
        }

        [Fact]
        public void NewDoctor_HasNoPatientsAssigned()
        {
            var doctor = CreateDoctor();

            // The collection is optional: it stays null until patients are attached.
            Assert.Null(doctor.Patients);
        }

        #endregion

        #region Optional profile fields

        [Fact]
        public void Doctor_DegreesHoldTheAcademicDegrees()
        {
            var doctor = CreateDoctor();

            Assert.NotNull(doctor.Degrees);
            Assert.Contains(DegreeType.Doctor, doctor.Degrees);
        }

        [Fact]
        public void Doctor_DegreesCanCarrySeveralDegrees()
        {
            var doctor = CreateDoctor();

            doctor.Degrees =
            [
                DegreeType.Doctor,
                DegreeType.Master,
                DegreeType.PhD,
            ];

            Assert.Equal(3, doctor.Degrees.Count);
            Assert.Equal(
                new[] { DegreeType.Doctor, DegreeType.Master, DegreeType.PhD },
                doctor.Degrees.ToArray()
            );
        }

        [Fact]
        public void Doctor_MediaIdIsNullByDefaultAndCanBeSet()
        {
            var doctor = CreateDoctor();

            Assert.Null(doctor.MediaId);

            var mediaId = Guid.CreateVersion7();
            doctor.MediaId = mediaId;

            Assert.Equal(mediaId, doctor.MediaId);
        }

        [Fact]
        public void Doctor_DateOfBirthIsNullByDefaultAndCanBeSet()
        {
            var doctor = CreateDoctor();

            Assert.Null(doctor.DateOfBirth);

            var dateOfBirth = new DateTimeOffset(1988, 1, 1, 0, 0, 0, TimeSpan.Zero);
            doctor.DateOfBirth = dateOfBirth;

            Assert.Equal(dateOfBirth, doctor.DateOfBirth);
        }

        #endregion

        #region Entity shape

        [Fact]
        public void Doctor_AndHeadDepartmentDoctor_AreBaseDoctorInstances()
        {
            var doctor = CreateDoctor();
            var head = CreateHeadDepartmentDoctor();

            Assert.IsAssignableFrom<BaseDoctor>(doctor);
            Assert.IsAssignableFrom<BaseDoctor>(head);
            Assert.True(typeof(BaseDoctor).IsAbstract);
        }

        [Fact]
        public void Doctor_AndHeadDepartmentDoctor_AreDifferentTypes()
        {
            var doctor = CreateDoctor();
            var head = CreateHeadDepartmentDoctor();

            Assert.IsNotType<HeadDepartmentDoctorEntity>(doctor);
            Assert.IsNotType<DoctorEntity>(head);
            Assert.False(typeof(DoctorEntity).IsAssignableFrom(typeof(HeadDepartmentDoctorEntity)));
            Assert.False(typeof(HeadDepartmentDoctorEntity).IsAssignableFrom(typeof(DoctorEntity)));
        }

        [Fact]
        public void NewDoctor_HasNoSearchVectorBecauseTheDatabaseGeneratesIt()
        {
            var doctor = CreateDoctor();
            var head = CreateHeadDepartmentDoctor();

            Assert.Null(doctor.SearchVector);
            Assert.Null(head.SearchVector);
        }

        #endregion

        #region Enums backing the domain model

        [Fact]
        public void ProfileRole_CoversBothProfileEntityTypes()
        {
            Assert.Equal(nameof(ProfileRole.Doctor), typeof(DoctorEntity).Name);
            Assert.Equal(
                nameof(ProfileRole.HeadDepartmentDoctor),
                typeof(HeadDepartmentDoctorEntity).Name
            );
            Assert.Equal(2, Enum.GetValues<ProfileRole>().Length);
        }

        [Fact]
        public void PositionType_StartsAtStaffDoctorAndEndsAtDirector()
        {
            Assert.Equal(1, (int)PositionType.StaffDoctor);
            Assert.Equal(6, (int)PositionType.Director);
            Assert.Equal(PositionType.StaffDoctor, Enum.GetValues<PositionType>().Min());
            Assert.Equal(PositionType.Director, Enum.GetValues<PositionType>().Max());
            Assert.Equal(6, Enum.GetValues<PositionType>().Length);
        }

        [Fact]
        public void AcademicTitle_HasNoneAssociateProfessorAndProfessor()
        {
            Assert.Equal(0, (int)AcademicTitle.None);
            Assert.Equal(1, (int)AcademicTitle.AssociateProfessor);
            Assert.Equal(2, (int)AcademicTitle.Professor);
            Assert.Equal(3, Enum.GetValues<AcademicTitle>().Length);
        }

        [Fact]
        public void GenderType_HasExactlyMaleAndFemale()
        {
            var values = Enum.GetValues<GenderType>().ToArray();

            Assert.Equal(2, values.Length);
            Assert.Equal(GenderType.Male, values[0]);
            Assert.Equal(GenderType.Female, values[1]);
            Assert.Equal(0, (int)GenderType.Male);
            Assert.Equal(1, (int)GenderType.Female);
        }

        [Fact]
        public void DegreeType_HasNineDeclaredMembersWithStableOrdinals()
        {
            (string Name, int Value)[] expected =
            [
                ("Professor", 0),
                ("Doctor", 1),
                ("Master", 2),
                ("Bachelor", 3),
                ("Associate", 4),
                ("Undergraduate", 5),
                ("Graduate", 6),
                ("PhD", 7),
                ("PostDoc", 8),
            ];

            var actual = Enum.GetValues<DegreeType>()
                .Select(x => (Name: x.ToString(), Value: (int)x))
                .ToArray();

            Assert.Equal(expected.Length, actual.Length);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i].Name, actual[i].Name);
                Assert.Equal(expected[i].Value, actual[i].Value);
            }
        }

        #endregion
    }
}

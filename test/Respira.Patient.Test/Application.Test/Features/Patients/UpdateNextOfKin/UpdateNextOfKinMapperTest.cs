using Respira.Patient.Application.Contracts.Mappers;
using Respira.Patient.Application.Features.Patients.UpdateNextOfKin;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Test.Features.Patients.UpdateNextOfKin
{
    public class UpdateNextOfKinMapperTest
    {
        /*
         * The mapper has no failure branch: it mutates the loaded entity in place
         * and always reports success, so only the happy path exists for it.
         * Rejection of invalid input is the validator's responsibility
         */
        private readonly IUpdateMapper<Domain.Entities.NextOfKin, UpdateNextOfKinCommand> _mapper =
            new UpdateNextOfKinMapper();

        private static readonly DateTimeOffset SeedCreatedAt = new(2026, 1, 15, 8, 30, 0, TimeSpan.Zero);

        #region Happy path

        [Fact]
        public void MapModel_FullCommand_UpdatesCommandFieldsAndStampsUpdatedAt()
        {
            var model = SeedKin();
            var command = new UpdateNextOfKinCommand
            {
                Id = model.Id,
                FullName = "Trần Đức Minh",
                PhoneNumber = "0913458769",
                Relationship = "Con trai",
            };
            var before = DateTimeOffset.UtcNow;

            var result = _mapper.MapModel(model, command);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);

            // The three command fields land on the entity
            Assert.Equal("Trần Đức Minh", model.FullName);
            Assert.Equal(command.PhoneNumber, model.PhoneNumber);
            Assert.Equal(command.Relationship, model.Relationship);

            // The update stamps UpdatedAt at mapping time
            Assert.InRange(model.UpdatedAt, before.AddMilliseconds(-1), DateTimeOffset.UtcNow.AddMilliseconds(1));

            // Identity, ownership and lifecycle fields are not part of the update
            Assert.Equal(SeedCreatedAt, model.CreatedAt);
            Assert.False(model.IsDeleted);
            Assert.NotEqual(Guid.Empty, model.Id);
            Assert.NotEqual(Guid.Empty, model.PatientId);
        }

        [Fact]
        public void MapModel_MessyFullName_NormalizedToVietnameseStyle()
        {
            /*
             * Business rule (Domain.Entities.NextOfKin): the FullName setter
             * normalizes on assignment - trimmed, inner whitespace collapsed and
             * every word capitalized
             */
            var model = SeedKin();

            var result = _mapper.MapModel(model, new UpdateNextOfKinCommand
            {
                Id = model.Id,
                FullName = "  nguyễn   thị bÍCH   vÂN ",
                PhoneNumber = model.PhoneNumber,
                Relationship = model.Relationship,
            });

            Assert.True(result.IsSuccess());
            Assert.Equal("Nguyễn Thị Bích Vân", model.FullName);
        }

        // The relationship is stored exactly as typed: FullName is the only member
        // of the entity that carries a normalizer
        [Theory]
        [InlineData("Mẹ")]
        [InlineData("Anh trai")]
        [InlineData("con gái")]
        public void MapModel_RelationshipVariants_PreservedExactly(string relationship)
        {
            var model = SeedKin();

            var result = _mapper.MapModel(model, new UpdateNextOfKinCommand
            {
                Id = model.Id,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Relationship = relationship,
            });

            Assert.True(result.IsSuccess());
            Assert.Equal(relationship, model.Relationship);
        }

        #endregion

        private static Domain.Entities.NextOfKin SeedKin() => new()
        {
            PatientId = Guid.CreateVersion7(),
            FullName = "Nguyễn Thị Mai Anh",
            PhoneNumber = "0987654321",
            Relationship = "Mẹ",
            CreatedAt = SeedCreatedAt,
        };
    }
}

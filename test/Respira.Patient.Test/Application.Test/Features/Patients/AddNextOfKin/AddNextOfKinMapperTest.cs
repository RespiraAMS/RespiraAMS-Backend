using Respira.Patient.Application.Contracts.Mappers;
using Respira.Patient.Application.Features.Patients.AddNextOfKin;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Test.Features.Patients.AddNextOfKin
{
    public class AddNextOfKinMapperTest
    {
        /*
         * The mapper has no failure branch: it only copies the command onto a fresh
         * entity, so only the happy path exists for it. Rejection of invalid input is
         * the validator's responsibility
         */
        private readonly ICreateMapper<AddNextOfKinCommand, Domain.Entities.NextOfKin> _mapper =
            new AddNextOfKinMapper();

        #region Happy path

        [Fact]
        public void ToModel_FullCommand_MapsEveryFieldAndCreatesEntity()
        {
            var command = ValidCommand();

            var result = _mapper.ToModel(command);

            Assert.True(result.IsSuccess());
            Assert.Null(result.Error);
            Assert.Equal(ApplicationStatus.Success, result.StatusCode);
            Assert.NotNull(result.Data);

            var model = result.Data;
            Assert.Equal(command.PatientId, model.PatientId);
            Assert.Equal(command.FullName, model.FullName);
            Assert.Equal(command.PhoneNumber, model.PhoneNumber);
            Assert.Equal(command.Relationship, model.Relationship);

            // Base generates the ID so the handler can return it right after saving
            Assert.NotEqual(Guid.Empty, model.Id);
            Assert.False(model.IsDeleted);
        }

        [Fact]
        public void ToModel_MessyFullName_NormalizedToVietnameseStyle()
        {
            /*
             * Business rule (Domain.Entities.NextOfKin): the FullName setter
             * normalizes on assignment - trimmed, inner whitespace collapsed and
             * every word capitalized
             */
            var result = _mapper.ToModel(ValidCommand() with
            {
                FullName = "  nguyễn   thị mAI   anh ",
            });

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);
            Assert.Equal("Nguyễn Thị Mai Anh", result.Data.FullName);
        }

        // The relationship is stored exactly as typed: FullName is the only member
        // of the entity that carries a normalizer
        [Theory]
        [InlineData("Mẹ")]
        [InlineData("Anh trai")]
        [InlineData("con gái")]
        public void ToModel_RelationshipVariants_PreservedExactly(string relationship)
        {
            var result = _mapper.ToModel(ValidCommand() with { Relationship = relationship });

            Assert.True(result.IsSuccess());
            Assert.NotNull(result.Data);
            Assert.Equal(relationship, result.Data.Relationship);
        }

        #endregion

        private static AddNextOfKinCommand ValidCommand() => new()
        {
            PatientId = Guid.CreateVersion7(),
            FullName = "Nguyễn Thị Mai Anh",
            PhoneNumber = "0987654321",
            Relationship = "Mẹ",
        };
    }
}

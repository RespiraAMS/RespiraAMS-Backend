using System.Text.Json;
using System.Text.Json.Serialization;
using Respira.Saga.Domain.Enums;

namespace Respira.Saga.Domain.Test.Enums
{
    /// <summary>
    /// The three step enums are persisted inside the saga state and travel over the message
    /// broker, so their member names and their ordinals are part of the wire contract. These
    /// tests freeze both down.
    /// </summary>
    public class SagaStepTest
    {
        private static readonly string[] CreateStepNames =
        [
            "CreatingAccount",
            "CreatingProfile",
            "CreatingMedia",
            "LinkingMedia",
            "SendingVerificationEmail",
            "Compensating",
            "Completed",
            "Failed",
        ];

        private static readonly string[] DeleteStepNames =
        [
            "DeletingProfile",
            "DeletingAccount",
            "DeletingMedia",
            "Completed",
            "Failed",
        ];

        private static readonly string[] UpdateStepNames =
        [
            "UpdatingAccount",
            "UploadingAvatar",
            "UpdatingProfile",
            "DeletingOldMedia",
            "Completed",
            "Failed",
        ];

        [Fact]
        public void CreateStep_HasExactlyTheEightDeclaredMembers()
        {
            var names = Enum.GetNames<CreateStep>();

            Assert.Equal(CreateStepNames.Length, names.Length);
            Assert.Equal(CreateStepNames, names);
        }

        [Fact]
        public void CreateStep_KeepsItsMemberOrdinals()
        {
            Assert.Equal(0, (int)CreateStep.CreatingAccount);
            Assert.Equal(1, (int)CreateStep.CreatingProfile);
            Assert.Equal(2, (int)CreateStep.CreatingMedia);
            Assert.Equal(3, (int)CreateStep.LinkingMedia);
            Assert.Equal(4, (int)CreateStep.SendingVerificationEmail);
            Assert.Equal(5, (int)CreateStep.Compensating);
            Assert.Equal(6, (int)CreateStep.Completed);
            Assert.Equal(7, (int)CreateStep.Failed);
        }

        [Fact]
        public void DeleteStep_HasExactlyTheFiveDeclaredMembers()
        {
            var names = Enum.GetNames<DeleteStep>();

            Assert.Equal(DeleteStepNames.Length, names.Length);
            Assert.Equal(DeleteStepNames, names);
        }

        [Fact]
        public void DeleteStep_KeepsItsMemberOrdinals()
        {
            Assert.Equal(0, (int)DeleteStep.DeletingProfile);
            Assert.Equal(1, (int)DeleteStep.DeletingAccount);
            Assert.Equal(2, (int)DeleteStep.DeletingMedia);
            Assert.Equal(3, (int)DeleteStep.Completed);
            Assert.Equal(4, (int)DeleteStep.Failed);
        }

        [Fact]
        public void UpdateStep_HasExactlyTheSixDeclaredMembers()
        {
            var names = Enum.GetNames<UpdateStep>();

            Assert.Equal(UpdateStepNames.Length, names.Length);
            Assert.Equal(UpdateStepNames, names);
        }

        [Fact]
        public void UpdateStep_KeepsItsMemberOrdinals()
        {
            Assert.Equal(0, (int)UpdateStep.UpdatingAccount);
            Assert.Equal(1, (int)UpdateStep.UploadingAvatar);
            Assert.Equal(2, (int)UpdateStep.UpdatingProfile);
            Assert.Equal(3, (int)UpdateStep.DeletingOldMedia);
            Assert.Equal(4, (int)UpdateStep.Completed);
            Assert.Equal(5, (int)UpdateStep.Failed);
        }

        [Fact]
        public void EveryMemberOfEveryStep_RoundTripsThroughEnumParse()
        {
            foreach (var name in CreateStepNames)
            {
                Assert.Equal(name, Enum.Parse<CreateStep>(name).ToString());
            }

            foreach (var name in DeleteStepNames)
            {
                Assert.Equal(name, Enum.Parse<DeleteStep>(name).ToString());
            }

            foreach (var name in UpdateStepNames)
            {
                Assert.Equal(name, Enum.Parse<UpdateStep>(name).ToString());
            }
        }

        [Fact]
        public void EveryMemberOfEveryStep_RoundTripsThroughEnumTryParseIgnoringCase()
        {
            foreach (var name in CreateStepNames)
            {
                Assert.True(Enum.TryParse(name, ignoreCase: true, out CreateStep createStep));
                Assert.Equal(name, createStep.ToString());

                Assert.True(Enum.TryParse(name.ToLowerInvariant(), ignoreCase: true, out createStep));
                Assert.Equal(name, createStep.ToString());
            }

            foreach (var name in DeleteStepNames)
            {
                Assert.True(Enum.TryParse(name, ignoreCase: true, out DeleteStep deleteStep));
                Assert.Equal(name, deleteStep.ToString());

                Assert.True(Enum.TryParse(name.ToUpperInvariant(), ignoreCase: true, out deleteStep));
                Assert.Equal(name, deleteStep.ToString());
            }

            foreach (var name in UpdateStepNames)
            {
                Assert.True(Enum.TryParse(name, ignoreCase: true, out UpdateStep updateStep));
                Assert.Equal(name, updateStep.ToString());

                Assert.True(Enum.TryParse(name.ToLowerInvariant(), ignoreCase: true, out updateStep));
                Assert.Equal(name, updateStep.ToString());
            }
        }

        [Fact]
        public void EnumGetValues_ReturnsExactlyTheDeclaredMembers()
        {
            Assert.Equal(
                CreateStepNames.OrderBy(name => name),
                Enum.GetValues<CreateStep>().Select(step => step.ToString()).OrderBy(name => name)
            );
            Assert.Equal(
                DeleteStepNames.OrderBy(name => name),
                Enum.GetValues<DeleteStep>().Select(step => step.ToString()).OrderBy(name => name)
            );
            Assert.Equal(
                UpdateStepNames.OrderBy(name => name),
                Enum.GetValues<UpdateStep>().Select(step => step.ToString()).OrderBy(name => name)
            );
        }

        [Fact]
        public void EnumIsDefined_RejectsAnUndefinedValue()
        {
            Assert.False(Enum.IsDefined(typeof(CreateStep), (CreateStep)99));
            Assert.False(Enum.IsDefined(typeof(DeleteStep), (DeleteStep)99));
            Assert.False(Enum.IsDefined(typeof(UpdateStep), (UpdateStep)99));
            Assert.False(Enum.IsDefined(typeof(CreateStep), "NotAMember"));
        }

        [Fact]
        public void EnumIsDefined_AcceptsEveryDeclaredValue()
        {
            foreach (var value in Enum.GetValues<CreateStep>())
            {
                Assert.True(Enum.IsDefined(typeof(CreateStep), value));
            }

            foreach (var value in Enum.GetValues<DeleteStep>())
            {
                Assert.True(Enum.IsDefined(typeof(DeleteStep), value));
            }

            foreach (var value in Enum.GetValues<UpdateStep>())
            {
                Assert.True(Enum.IsDefined(typeof(UpdateStep), value));
            }
        }

        [Fact]
        public void StepEnums_SerialiseToTheirNames()
        {
            Assert.Equal("\"Compensating\"", JsonSerializer.Serialize(CreateStep.Compensating));
            Assert.Equal("\"Completed\"", JsonSerializer.Serialize(CreateStep.Completed));
            Assert.Equal("\"DeletingMedia\"", JsonSerializer.Serialize(DeleteStep.DeletingMedia));
            Assert.Equal("\"UploadingAvatar\"", JsonSerializer.Serialize(UpdateStep.UploadingAvatar));
        }

        [Fact]
        public void StepEnums_DeserialiseFromTheirNames()
        {
            Assert.Equal(CreateStep.Compensating, JsonSerializer.Deserialize<CreateStep>("\"Compensating\""));
            Assert.Equal(DeleteStep.Failed, JsonSerializer.Deserialize<DeleteStep>("\"Failed\""));
            Assert.Equal(UpdateStep.UpdatingProfile, JsonSerializer.Deserialize<UpdateStep>("\"UpdatingProfile\""));
        }

        [Fact]
        public void StepEnums_AreAnnotatedWithTheStringEnumConverter()
        {
            Assert.True(typeof(CreateStep).IsDefined(typeof(JsonConverterAttribute), inherit: false));
            Assert.True(typeof(DeleteStep).IsDefined(typeof(JsonConverterAttribute), inherit: false));
            Assert.True(typeof(UpdateStep).IsDefined(typeof(JsonConverterAttribute), inherit: false));

            var converter = (JsonConverterAttribute)typeof(CreateStep)
                .GetCustomAttributes(typeof(JsonConverterAttribute), inherit: false)
                .Single();

            Assert.Equal(typeof(JsonStringEnumConverter), converter.ConverterType);
        }

        [Fact]
        public void StepEnums_AreValueTypesWithTheirOwnNamespaces()
        {
            Assert.True(typeof(CreateStep).IsValueType);
            Assert.True(typeof(DeleteStep).IsValueType);
            Assert.True(typeof(UpdateStep).IsValueType);

            Assert.Equal("Respira.Saga.Domain.Enums.CreateStep", typeof(CreateStep).FullName);
            Assert.Equal("Respira.Saga.Domain.Enums.DeleteStep", typeof(DeleteStep).FullName);
            Assert.Equal("Respira.Saga.Domain.Enums.UpdateStep", typeof(UpdateStep).FullName);
        }
    }
}

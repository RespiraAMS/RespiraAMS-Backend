using Authentication.Application.Features.Accounts.Commands.Create;
using Authentication.Application.Features.Accounts.Commands.Create.Events;
using Authentication.Application.Features.Authentication.Commands.SendVerify;
using Authentication.Application.Features.Accounts.Commands.Update;
using Authentication.Domain.Enums;
using Moq;
using Respira.Doctor.Application.Features.Doctors.Commands.Create;
using Respira.Doctor.Application.Features.Doctors.Commands.Create.Events;
using Respira.Doctor.Domain.Enums;
using Respira.Saga.Application.Features.Create.Commands;
using Respira.Saga.Application.Features.Delete.Commands;
using Respira.Saga.Application.Features.Update.Commands;
using Respira.Saga.Domain.Enums;
using Authentication.Application.Features.Accounts.Commands.SoftDelete;
using Respira.Doctor.Application.Features.Doctors.Commands.SoftDelete;
using Wolverine;

namespace Respira.Saga.Application.Test.Features.Create;

public sealed class CreateSagaTest
{
    [Fact]
    public async Task StartAsync_NormalizesEmailAndDoesNotKeepPlaintextPasswordInState()
    {
        var bus = CreateBus();
        var saga = new CreateSaga();
        var command = new StartCreateCommand
        {
            SagaId = Guid.CreateVersion7(),
            CreatorAccountId = Guid.CreateVersion7(),
            CreatorRole = RoleType.Admin,
            Email = "  Reviewer@Example.test ",
            Password = "Secret-123",
            Phone = "0900000000",
            Role = RoleType.MedicalRegulationReviewer,
        };

        await saga.StartAsync(command, bus.Object);

        Assert.Equal("reviewer@example.test", saga.Email);
        Assert.Equal(CreateStep.CreatingAccount, saga.Step);
        Assert.Null(typeof(CreateSaga).GetProperty(nameof(command.Password)));
        bus.Verify(messageBus => messageBus.SendAsync(
            It.Is<CreateAccountCommand>(message => message.Password == "Secret-123"
                && message.Email == "reviewer@example.test"
                && message.Role == RoleType.MedicalRegulationReviewer),
            It.IsAny<DeliveryOptions>()
        ), Times.Once);
    }

    [Fact]
    public async Task MedicalRegulationReviewerAccountCreated_SendsVerificationWithoutDoctorProfile()
    {
        var bus = CreateBus();
        var saga = new CreateSaga
        {
            SagaId = Guid.CreateVersion7(),
            Role = RoleType.MedicalRegulationReviewer,
            Email = "reviewer@example.test",
            CreatorRole = RoleType.Admin,
        };

        await saga.HandleAsync(new CreateAccountCompleted
        {
            SagaId = saga.SagaId,
            AccountId = Guid.CreateVersion7(),
        }, bus.Object);

        bus.Verify(messageBus => messageBus.SendAsync(
            It.Is<SendVerifyCommand>(message => message.Email == saga.Email
                && message.TargetAccountId == saga.AccountId),
            It.IsAny<DeliveryOptions>()
        ), Times.Once);
        bus.Verify(messageBus => messageBus.SendAsync(
            It.IsAny<CreateDoctorProfileCommand>(),
            It.IsAny<DeliveryOptions>()
        ), Times.Never);
    }

    [Fact]
    public async Task HeadDepartmentDoctorCreator_IsAssignedToCreatedDoctor()
    {
        var bus = CreateBus();
        var creatorId = Guid.CreateVersion7();
        var saga = new CreateSaga
        {
            SagaId = Guid.CreateVersion7(),
            CreatorAccountId = creatorId,
            CreatorRole = RoleType.HeadDepartmentDoctor,
            Role = RoleType.Doctor,
        };

        await saga.HandleAsync(new CreateAccountCompleted
        {
            SagaId = saga.SagaId,
            AccountId = Guid.CreateVersion7(),
        }, bus.Object);

        bus.Verify(messageBus => messageBus.SendAsync(
            It.Is<CreateDoctorProfileCommand>(message =>
                message.Role == ProfileRole.Doctor
                && message.HeadDepartmentDoctorAccountId == creatorId),
            It.IsAny<DeliveryOptions>()
        ), Times.Once);
    }

    [Fact]
    public async Task UpdateSaga_StartAsync_SendsAccountUpdateAndKeepsPasswordOutOfState()
    {
        var bus = CreateBus();
        var saga = new UpdateSaga();
        var command = new StartUpdateCommand
        {
            SagaId = Guid.CreateVersion7(),
            ActorRole = "Admin",
            AccountId = Guid.CreateVersion7(),
            DoctorId = Guid.CreateVersion7(),
            Email = " Doctor@Example.test ",
            Phone = "0900000000",
            Password = "Replacement-123",
        };

        await saga.StartAsync(command, bus.Object);

        Assert.Equal("doctor@example.test", saga.Email);
        Assert.Equal(UpdateStep.UpdatingAccount, saga.Step);
        Assert.Null(typeof(UpdateSaga).GetProperty(nameof(command.Password)));
        bus.Verify(messageBus => messageBus.SendAsync(
            It.Is<UpdateAccountCommand>(message => message.Password == "Replacement-123"
                && message.Email == "doctor@example.test"
                && message.AccountId == command.AccountId),
            It.IsAny<DeliveryOptions>()
        ), Times.Once);
    }

    [Fact]
    public async Task DeleteSaga_StartAsync_BeginsWithDoctorProfileSoftDelete()
    {
        var bus = CreateBus();
        var saga = new DeleteSaga();
        var command = new StartDeleteCommand
        {
            SagaId = Guid.CreateVersion7(),
            ActorAccountId = Guid.CreateVersion7(),
            ActorRole = "HeadDepartmentDoctor",
            AccountId = Guid.CreateVersion7(),
            DoctorId = Guid.CreateVersion7(),
            MediaId = Guid.CreateVersion7(),
        };

        await saga.StartAsync(command, bus.Object);

        Assert.Equal(DeleteStep.DeletingProfile, saga.Step);
        bus.Verify(messageBus => messageBus.SendAsync(
            It.Is<SoftDeleteDoctorCommand>(message => message.SagaId == command.SagaId
                && message.DoctorId == command.DoctorId
                && message.TargetAccountId == command.AccountId),
            It.IsAny<DeliveryOptions>()
        ), Times.Once);
    }

    private static Mock<IMessageBus> CreateBus()
    {
        var bus = new Mock<IMessageBus>();
        bus.Setup(messageBus => messageBus.SendAsync(
            It.IsAny<CreateAccountCommand>(), It.IsAny<DeliveryOptions>()))
            .Returns(ValueTask.CompletedTask);
        bus.Setup(messageBus => messageBus.SendAsync(
            It.IsAny<CreateDoctorProfileCommand>(), It.IsAny<DeliveryOptions>()))
            .Returns(ValueTask.CompletedTask);
        bus.Setup(messageBus => messageBus.SendAsync(
            It.IsAny<SendVerifyCommand>(), It.IsAny<DeliveryOptions>()))
            .Returns(ValueTask.CompletedTask);
        bus.Setup(messageBus => messageBus.SendAsync(
            It.IsAny<UpdateAccountCommand>(), It.IsAny<DeliveryOptions>()))
            .Returns(ValueTask.CompletedTask);
        bus.Setup(messageBus => messageBus.SendAsync(
            It.IsAny<SoftDeleteDoctorCommand>(), It.IsAny<DeliveryOptions>()))
            .Returns(ValueTask.CompletedTask);
        return bus;
    }
}

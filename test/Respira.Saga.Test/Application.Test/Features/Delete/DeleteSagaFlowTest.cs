using Microsoft.Extensions.Logging;
using Moq;
using Respira.Saga.Application.Contracts.Media;
using Respira.Saga.Application.Contracts.Messages.Authentication.SoftDelete;
using Respira.Saga.Application.Contracts.Messages.Authentication.SoftDelete.Events;
using Respira.Saga.Application.Contracts.Messages.Doctor.SoftDelete;
using Respira.Saga.Application.Contracts.Messages.Doctor.SoftDelete.Events;
using Respira.Saga.Application.Features.Delete.Commands;
using Respira.Saga.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Saga.Application.Test.Features.Delete;

/// <summary>
/// Covers the delete saga: the soft delete of the profile, the soft delete of the account
/// and the removal of the avatar's media asset. The delete flow never compensates: every
/// step is idempotent, so a failure is reported as <see cref="DeleteStep.Failed"/>.
/// </summary>
public sealed class DeleteSagaFlowTest
{
    [Fact]
    public async Task StartAsync_SendsSoftDeleteDoctorCommandWithEveryTarget()
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
                && message.TargetAccountId == command.AccountId
                && message.ActorAccountId == command.ActorAccountId
                && message.ActorRole == command.ActorRole),
            It.IsAny<DeliveryOptions>()
        ), Times.Once);
    }

    [Fact]
    public async Task StartAsync_CopiesTheTargetsIntoTheState()
    {
        var bus = CreateBus();
        var saga = new DeleteSaga();
        var command = new StartDeleteCommand
        {
            SagaId = Guid.CreateVersion7(),
            ActorAccountId = Guid.CreateVersion7(),
            ActorRole = "Admin",
            AccountId = Guid.CreateVersion7(),
            DoctorId = Guid.CreateVersion7(),
            MediaId = Guid.CreateVersion7(),
        };

        await saga.StartAsync(command, bus.Object);

        Assert.Equal(command.SagaId, saga.SagaId);
        Assert.Equal(command.AccountId, saga.AccountId);
        Assert.Equal(command.DoctorId, saga.DoctorId);
        Assert.Equal(command.MediaId, saga.MediaId);
        Assert.Equal(command.ActorAccountId, saga.ActorAccountId);
        Assert.Equal("Admin", saga.ActorRole);
    }

    [Fact]
    public async Task SoftDeleteDoctorCompleted_SendsAccountSoftDelete()
    {
        var bus = CreateBus();
        var saga = CreateSaga();

        await saga.HandleAsync(
            new SoftDeleteDoctorCompleted(saga.SagaId, saga.DoctorId),
            bus.Object
        );

        Assert.Equal(DeleteStep.DeletingAccount, saga.Step);
        bus.Verify(messageBus => messageBus.SendAsync(
            It.Is<SoftDeleteCommand>(message => message.AccountId == saga.AccountId
                && message.SagaId == saga.SagaId
                && message.ActorAccountId == saga.ActorAccountId
                && message.ActorRole == saga.ActorRole),
            It.IsAny<DeliveryOptions>()
        ), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteDoctorFailure_FailsWithTheReason()
    {
        var saga = CreateSaga();

        await saga.HandleAsync(
            new SoftDeleteDoctorFailure(saga.SagaId, "the profile is locked")
        );

        Assert.Equal(DeleteStep.Failed, saga.Step);
        Assert.Equal("the profile is locked", saga.FailureReason);
    }

    [Fact]
    public async Task SoftDeleteDoctorFailure_DoesNotSendTheAccountSoftDelete()
    {
        var bus = CreateBus();
        var saga = CreateSaga();

        await saga.HandleAsync(
            new SoftDeleteDoctorFailure(saga.SagaId, "the profile is locked")
        );

        bus.Verify(messageBus => messageBus.SendAsync(
            It.IsAny<SoftDeleteCommand>(),
            It.IsAny<DeliveryOptions>()
        ), Times.Never);
    }

    [Fact]
    public async Task SoftDeleteCompleted_WithMedia_DeletesTheMediaAsset()
    {
        var mediaClient = CreateMediaClient(Result<Guid>.Success(ApplicationStatus.Success, Guid.NewGuid()));
        var saga = CreateSaga();
        var mediaId = Guid.NewGuid();
        saga.MediaId = mediaId;

        await saga.HandleAsync(
            new SoftDeleteCompleted(saga.SagaId, saga.AccountId),
            mediaClient.Object,
            Mock.Of<ILogger<DeleteSaga>>(),
            TestContext.Current.CancellationToken
        );

        mediaClient.Verify(client => client.DeleteAssetAsync(
            mediaId,
            It.IsAny<CancellationToken>(),
            It.IsAny<Guid?>(),
            It.IsAny<string>()
        ), Times.Once);
        Assert.Equal(DeleteStep.Completed, saga.Step);
    }

    [Fact]
    public async Task SoftDeleteCompleted_WithoutMedia_SkipsTheMediaDeletion()
    {
        var mediaClient = new Mock<IMediaClient>();
        var saga = CreateSaga();
        saga.MediaId = Guid.Empty;

        await saga.HandleAsync(
            new SoftDeleteCompleted(saga.SagaId, saga.AccountId),
            mediaClient.Object,
            Mock.Of<ILogger<DeleteSaga>>(),
            TestContext.Current.CancellationToken
        );

        mediaClient.Verify(client => client.DeleteAssetAsync(
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Guid?>(),
            It.IsAny<string>()
        ), Times.Never);
        Assert.Equal(DeleteStep.Completed, saga.Step);
    }

    [Fact]
    public async Task SoftDeleteCompleted_WhenMediaDeletionFails_Fails()
    {
        var mediaClient = CreateMediaClient(
            Result<Guid>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "nope"))
        );
        var saga = CreateSaga();
        saga.MediaId = Guid.NewGuid();

        await saga.HandleAsync(
            new SoftDeleteCompleted(saga.SagaId, saga.AccountId),
            mediaClient.Object,
            Mock.Of<ILogger<DeleteSaga>>(),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(DeleteStep.Failed, saga.Step);
        Assert.Equal("nope", saga.FailureReason);
    }

    [Fact]
    public async Task SoftDeleteCompleted_WhenMediaDeletionThrows_FailsWithoutThrowing()
    {
        var mediaClient = new Mock<IMediaClient>();
        mediaClient
            .Setup(client => client.DeleteAssetAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>()
            ))
            .ThrowsAsync(new InvalidOperationException("the media service is down"));
        var saga = CreateSaga();
        saga.MediaId = Guid.NewGuid();

        await saga.HandleAsync(
            new SoftDeleteCompleted(saga.SagaId, saga.AccountId),
            mediaClient.Object,
            Mock.Of<ILogger<DeleteSaga>>(),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(DeleteStep.Failed, saga.Step);
        Assert.Equal("the media service is down", saga.FailureReason);
    }

    [Fact]
    public async Task SoftDeleteFailure_FailsWithTheReason()
    {
        var saga = CreateSaga();

        await saga.HandleAsync(new SoftDeleteFailure(saga.SagaId, "the account is locked"));

        Assert.Equal(DeleteStep.Failed, saga.Step);
        Assert.Equal("the account is locked", saga.FailureReason);
    }

    [Fact]
    public async Task NotFound_SoftDeleteDoctorCompleted_DoesNothing()
    {
        var handled = DeleteSaga.NotFound(new SoftDeleteDoctorCompleted(Guid.NewGuid(), Guid.NewGuid()));

        await handled;

        Assert.True(handled.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task NotFound_SoftDeleteDoctorFailure_DoesNothing()
    {
        var handled = DeleteSaga.NotFound(new SoftDeleteDoctorFailure(Guid.NewGuid(), "boom"));

        await handled;

        Assert.True(handled.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task NotFound_SoftDeleteCompleted_DoesNothing()
    {
        var handled = DeleteSaga.NotFound(new SoftDeleteCompleted(Guid.NewGuid(), Guid.NewGuid()));

        await handled;

        Assert.True(handled.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task NotFound_SoftDeleteFailure_DoesNothing()
    {
        var handled = DeleteSaga.NotFound(new SoftDeleteFailure(Guid.NewGuid(), "boom"));

        await handled;

        Assert.True(handled.IsCompletedSuccessfully);
    }

    private static DeleteSaga CreateSaga() => new()
    {
        SagaId = Guid.CreateVersion7(),
        ActorAccountId = Guid.CreateVersion7(),
        ActorRole = "Admin",
        AccountId = Guid.CreateVersion7(),
        DoctorId = Guid.CreateVersion7(),
        MediaId = Guid.NewGuid(),
    };

    private static Mock<IMediaClient> CreateMediaClient(Result<Guid> deletionResult)
    {
        var mediaClient = new Mock<IMediaClient>();
        mediaClient
            .Setup(client => client.DeleteAssetAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>()
            ))
            .ReturnsAsync(deletionResult);
        return mediaClient;
    }

    private static Mock<IMessageBus> CreateBus()
    {
        var bus = new Mock<IMessageBus>();
        bus.Setup(messageBus => messageBus.SendAsync(
            It.IsAny<SoftDeleteDoctorCommand>(), It.IsAny<DeliveryOptions>()))
            .Returns(ValueTask.CompletedTask);
        bus.Setup(messageBus => messageBus.SendAsync(
            It.IsAny<SoftDeleteCommand>(), It.IsAny<DeliveryOptions>()))
            .Returns(ValueTask.CompletedTask);
        return bus;
    }
}

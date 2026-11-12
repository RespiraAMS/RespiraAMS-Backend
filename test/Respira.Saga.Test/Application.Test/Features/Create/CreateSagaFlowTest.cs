using Microsoft.Extensions.Logging;
using Moq;
using Respira.Authentication.Domain.Enums;
using Respira.Doctor.Domain.Enums;
using Respira.Saga.Application.Contracts.Media;
using Respira.Saga.Application.Contracts.Messages.Authentication;
using Respira.Saga.Application.Contracts.Messages.Authentication.Create;
using Respira.Saga.Application.Contracts.Messages.Authentication.Create.Events;
using Respira.Saga.Application.Contracts.Messages.Authentication.Events;
using Respira.Saga.Application.Contracts.Messages.Authentication.SoftDelete;
using Respira.Saga.Application.Contracts.Messages.Authentication.SoftDelete.Events;
using Respira.Saga.Application.Contracts.Messages.Doctor.Create;
using Respira.Saga.Application.Contracts.Messages.Doctor.Create.Events;
using Respira.Saga.Application.Contracts.Messages.Doctor.SoftDelete;
using Respira.Saga.Application.Contracts.Messages.Doctor.SoftDelete.Events;
using Respira.Saga.Application.Contracts.Messages.Doctor.Update;
using Respira.Saga.Application.Contracts.Messages.Doctor.Update.Events;
using Respira.Saga.Application.Features.Create.Commands;
using Respira.Saga.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Saga.Application.Test.Features.Create;

/// <summary>
/// Covers the media and compensation branches of the create saga that
/// <see cref="CreateSagaTest"/> does not reach: the avatar upload, the rollback of the
/// created profile/account and the terminal states of the flow.
/// </summary>
public sealed class CreateSagaFlowTest
{
    [Fact]
    public async Task DoctorProfileCreated_WithoutTempAvatarPath_DoesNotUploadAndCompensates()
    {
        var bus = CreateBus();
        var mediaClient = new Mock<IMediaClient>();
        var saga = CreateSagaState();
        saga.TempAvatarPath = null;

        await saga.HandleAsync(
            CreateProfileCreatedEvent(saga),
            bus.Object,
            mediaClient.Object,
            Mock.Of<ILogger<CreateSaga>>(),
            TestContext.Current.CancellationToken
        );

        mediaClient.Verify(
            client => client.UploadAvatarAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>()
            ),
            Times.Never
        );
        Assert.Equal(CreateStep.Compensating, saga.Step);
        Assert.Equal("The avatar file is missing", saga.FailureReason);
        bus.Verify(
            messageBus => messageBus.SendAsync(
                It.Is<SoftDeleteDoctorCommand>(message => message.DoctorId == saga.DoctorId
                    && message.SagaId == saga.SagaId),
                It.IsAny<DeliveryOptions>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task DoctorProfileCreated_WithWhitespaceTempAvatarPath_DoesNotUploadAndCompensates()
    {
        var bus = CreateBus();
        var mediaClient = new Mock<IMediaClient>();
        var saga = CreateSagaState();
        saga.TempAvatarPath = "   ";

        await saga.HandleAsync(
            CreateProfileCreatedEvent(saga),
            bus.Object,
            mediaClient.Object,
            Mock.Of<ILogger<CreateSaga>>(),
            TestContext.Current.CancellationToken
        );

        mediaClient.Verify(
            client => client.UploadAvatarAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>()
            ),
            Times.Never
        );
        Assert.Equal(CreateStep.Compensating, saga.Step);
        Assert.Equal("The avatar file is missing", saga.FailureReason);
    }

    [Fact]
    public async Task DoctorProfileCreated_WithMissingTempAvatarFile_DoesNotUploadAndCompensates()
    {
        var bus = CreateBus();
        var mediaClient = new Mock<IMediaClient>();
        var saga = CreateSagaState();
        saga.TempAvatarPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.png");

        await saga.HandleAsync(
            CreateProfileCreatedEvent(saga),
            bus.Object,
            mediaClient.Object,
            Mock.Of<ILogger<CreateSaga>>(),
            TestContext.Current.CancellationToken
        );

        mediaClient.Verify(
            client => client.UploadAvatarAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>()
            ),
            Times.Never
        );
        Assert.Equal(CreateStep.Compensating, saga.Step);
        Assert.Equal("The avatar file is missing", saga.FailureReason);
    }

    [Fact]
    public async Task DoctorProfileCreated_WithRealTempFile_UploadsAvatarAndLinksMedia()
    {
        var bus = CreateBus();
        var mediaClient = CreateMediaClient(Result<Guid>.Success(ApplicationStatus.Success, Guid.NewGuid()));
        var saga = CreateSagaState();
        var path = CreateTempAvatar();
        saga.TempAvatarPath = path;
        var mediaId = Guid.NewGuid();
        mediaClient
            .Setup(client => client.UploadAvatarAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>()
            ))
            .ReturnsAsync(Result<Guid>.Success(ApplicationStatus.Success, mediaId));

        try
        {
            await saga.HandleAsync(
                CreateProfileCreatedEvent(saga),
                bus.Object,
                mediaClient.Object,
                Mock.Of<ILogger<CreateSaga>>(),
                TestContext.Current.CancellationToken
            );

            mediaClient.Verify(
                client => client.UploadAvatarAsync(
                    It.IsAny<Stream>(),
                    Path.GetFileName(path),
                    $"doctors/{saga.DoctorId}/avatar",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<string>()
                ),
                Times.Once
            );
            Assert.Equal(mediaId, saga.MediaId);
            Assert.Equal(CreateStep.LinkingMedia, saga.Step);
            bus.Verify(
                messageBus => messageBus.SendAsync(
                    It.Is<UpdateDoctorMediaIdCommand>(message => message.MediaId == mediaId
                        && message.DoctorId == saga.DoctorId
                        && message.SagaId == saga.SagaId),
                    It.IsAny<DeliveryOptions>()
                ),
                Times.Once
            );
        }
        finally
        {
            DeleteTempAvatar(path);
        }
    }

    [Fact]
    public async Task DoctorProfileCreated_AfterSuccessfulUpload_DeletesTempAvatar()
    {
        var bus = CreateBus();
        var mediaClient = CreateMediaClient(Result<Guid>.Success(ApplicationStatus.Success, Guid.NewGuid()));
        var saga = CreateSagaState();
        var path = CreateTempAvatar();
        saga.TempAvatarPath = path;

        try
        {
            await saga.HandleAsync(
                CreateProfileCreatedEvent(saga),
                bus.Object,
                mediaClient.Object,
                Mock.Of<ILogger<CreateSaga>>(),
                TestContext.Current.CancellationToken
            );

            Assert.False(File.Exists(path));
        }
        finally
        {
            DeleteTempAvatar(path);
        }
    }

    [Fact]
    public async Task DoctorProfileCreated_AfterFailedUpload_DeletesTempAvatar()
    {
        var bus = CreateBus();
        var mediaClient = CreateMediaClient(
            Result<Guid>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "nope"))
        );
        var saga = CreateSagaState();
        var path = CreateTempAvatar();
        saga.TempAvatarPath = path;

        try
        {
            await saga.HandleAsync(
                CreateProfileCreatedEvent(saga),
                bus.Object,
                mediaClient.Object,
                Mock.Of<ILogger<CreateSaga>>(),
                TestContext.Current.CancellationToken
            );

            Assert.False(File.Exists(path));
            Assert.Equal(CreateStep.Compensating, saga.Step);
        }
        finally
        {
            DeleteTempAvatar(path);
        }
    }

    [Fact]
    public async Task DoctorProfileCreated_WhenUploadFails_Compensates()
    {
        var bus = CreateBus();
        var mediaClient = CreateMediaClient(
            Result<Guid>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "nope"))
        );
        var saga = CreateSagaState();
        var path = CreateTempAvatar();
        saga.TempAvatarPath = path;

        try
        {
            await saga.HandleAsync(
                CreateProfileCreatedEvent(saga),
                bus.Object,
                mediaClient.Object,
                Mock.Of<ILogger<CreateSaga>>(),
                TestContext.Current.CancellationToken
            );

            Assert.Equal(CreateStep.Compensating, saga.Step);
            Assert.Equal("nope", saga.FailureReason);
            bus.Verify(
                messageBus => messageBus.SendAsync(
                    It.Is<SoftDeleteDoctorCommand>(message => message.DoctorId == saga.DoctorId),
                    It.IsAny<DeliveryOptions>()
                ),
                Times.Once
            );
        }
        finally
        {
            DeleteTempAvatar(path);
        }
    }

    [Fact]
    public async Task DoctorProfileCreated_WhenUploadThrows_DoesNotThrowAndCompensates()
    {
        var bus = CreateBus();
        var mediaClient = new Mock<IMediaClient>();
        mediaClient
            .Setup(client => client.UploadAvatarAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>()
            ))
            .ThrowsAsync(new InvalidOperationException("the media service is down"));
        var saga = CreateSagaState();
        var path = CreateTempAvatar();
        saga.TempAvatarPath = path;

        try
        {
            await saga.HandleAsync(
                CreateProfileCreatedEvent(saga),
                bus.Object,
                mediaClient.Object,
                Mock.Of<ILogger<CreateSaga>>(),
                TestContext.Current.CancellationToken
            );

            Assert.Equal(CreateStep.Compensating, saga.Step);
            Assert.Equal("the media service is down", saga.FailureReason);
            bus.Verify(
                messageBus => messageBus.SendAsync(
                    It.Is<SoftDeleteDoctorCommand>(message => message.DoctorId == saga.DoctorId),
                    It.IsAny<DeliveryOptions>()
                ),
                Times.Once
            );
        }
        finally
        {
            DeleteTempAvatar(path);
        }
    }

    [Fact]
    public async Task DoctorProfileCreated_WhenUploadReturnsEmptyGuid_Compensates()
    {
        var bus = CreateBus();
        var mediaClient = CreateMediaClient(Result<Guid>.Success(ApplicationStatus.Success, Guid.Empty));
        var saga = CreateSagaState();
        var path = CreateTempAvatar();
        saga.TempAvatarPath = path;

        try
        {
            await saga.HandleAsync(
                CreateProfileCreatedEvent(saga),
                bus.Object,
                mediaClient.Object,
                Mock.Of<ILogger<CreateSaga>>(),
                TestContext.Current.CancellationToken
            );

            Assert.Equal(CreateStep.Compensating, saga.Step);
            Assert.Equal("The avatar could not be uploaded", saga.FailureReason);
            bus.Verify(
                messageBus => messageBus.SendAsync(
                    It.Is<SoftDeleteDoctorCommand>(message => message.DoctorId == saga.DoctorId),
                    It.IsAny<DeliveryOptions>()
                ),
                Times.Once
            );
        }
        finally
        {
            DeleteTempAvatar(path);
        }
    }

    [Fact]
    public async Task Compensation_WithDoctorId_SendsSoftDeleteDoctorCommand()
    {
        var bus = CreateBus();
        var saga = CreateSagaState();
        saga.DoctorId = Guid.NewGuid();

        await saga.HandleAsync(
            new DoctorProfileFailure(saga.SagaId, "the profile was rejected"),
            bus.Object
        );

        Assert.Equal(CreateStep.Compensating, saga.Step);
        Assert.Equal("the profile was rejected", saga.FailureReason);
        bus.Verify(
            messageBus => messageBus.SendAsync(
                It.Is<SoftDeleteDoctorCommand>(message => message.DoctorId == saga.DoctorId
                    && message.SagaId == saga.SagaId),
                It.IsAny<DeliveryOptions>()
            ),
            Times.Once
        );
        bus.Verify(
            messageBus => messageBus.SendAsync(
                It.IsAny<SoftDeleteCommand>(),
                It.IsAny<DeliveryOptions>()
            ),
            Times.Never
        );
    }

    [Fact]
    public async Task Compensation_WithoutDoctorId_SendsAccountSoftDelete()
    {
        var bus = CreateBus();
        var saga = CreateSagaState();
        saga.DoctorId = Guid.Empty;
        saga.AccountId = Guid.NewGuid();

        await saga.HandleAsync(
            new DoctorProfileFailure(saga.SagaId, "the profile was rejected"),
            bus.Object
        );

        Assert.Equal(CreateStep.Compensating, saga.Step);
        bus.Verify(
            messageBus => messageBus.SendAsync(
                It.Is<SoftDeleteCommand>(message => message.AccountId == saga.AccountId
                    && message.SagaId == saga.SagaId),
                It.IsAny<DeliveryOptions>()
            ),
            Times.Once
        );
        bus.Verify(
            messageBus => messageBus.SendAsync(
                It.IsAny<SoftDeleteDoctorCommand>(),
                It.IsAny<DeliveryOptions>()
            ),
            Times.Never
        );
    }

    [Fact]
    public async Task Compensation_WithoutDoctorIdAndAccountId_Fails()
    {
        var bus = CreateBus();
        var saga = CreateSagaState();
        saga.DoctorId = Guid.Empty;
        saga.AccountId = Guid.Empty;

        await saga.HandleAsync(
            new DoctorProfileFailure(saga.SagaId, "nothing was created"),
            bus.Object
        );

        Assert.Equal(CreateStep.Failed, saga.Step);
        Assert.Equal("nothing was created", saga.FailureReason);
        bus.Verify(
            messageBus => messageBus.SendAsync(
                It.IsAny<SoftDeleteDoctorCommand>(),
                It.IsAny<DeliveryOptions>()
            ),
            Times.Never
        );
        bus.Verify(
            messageBus => messageBus.SendAsync(
                It.IsAny<SoftDeleteCommand>(),
                It.IsAny<DeliveryOptions>()
            ),
            Times.Never
        );
    }

    [Fact]
    public async Task DoctorProfileFailure_Compensates()
    {
        var bus = CreateBus();
        var saga = CreateSagaState();
        saga.DoctorId = Guid.NewGuid();

        await saga.HandleAsync(
            new DoctorProfileFailure(saga.SagaId, "the profile was rejected"),
            bus.Object
        );

        Assert.Equal(CreateStep.Compensating, saga.Step);
        Assert.Equal("the profile was rejected", saga.FailureReason);
        bus.Verify(
            messageBus => messageBus.SendAsync(
                It.Is<SoftDeleteDoctorCommand>(message => message.DoctorId == saga.DoctorId),
                It.IsAny<DeliveryOptions>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task DoctorMediaUpdateFailure_Compensates()
    {
        var bus = CreateBus();
        var saga = CreateSagaState();
        saga.DoctorId = Guid.NewGuid();

        await saga.HandleAsync(
            new DoctorMediaUpdateFailure(saga.SagaId, "the media could not be linked"),
            bus.Object
        );

        Assert.Equal(CreateStep.Compensating, saga.Step);
        Assert.Equal("the media could not be linked", saga.FailureReason);
        bus.Verify(
            messageBus => messageBus.SendAsync(
                It.Is<SoftDeleteDoctorCommand>(message => message.DoctorId == saga.DoctorId),
                It.IsAny<DeliveryOptions>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task DoctorMediaUpdated_SendsVerificationEmail()
    {
        var bus = CreateBus();
        var saga = CreateSagaState();
        var mediaId = Guid.NewGuid();

        await saga.HandleAsync(
            new DoctorMediaUpdated(saga.SagaId, saga.DoctorId, mediaId),
            bus.Object
        );

        Assert.Equal(mediaId, saga.MediaId);
        Assert.Equal(CreateStep.SendingVerificationEmail, saga.Step);
        bus.Verify(
            messageBus => messageBus.SendAsync(
                It.Is<SendVerifyCommand>(message => message.Email == saga.Email
                    && message.TargetAccountId == saga.AccountId),
                It.IsAny<DeliveryOptions>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task SendVerifyCompleted_Completes()
    {
        var saga = CreateSagaState();

        await saga.HandleAsync(new SendVerifyCompleted(saga.SagaId, saga.Email));

        Assert.Equal(CreateStep.Completed, saga.Step);
        Assert.Null(saga.FailureReason);
    }

    [Fact]
    public async Task SendVerifyFailure_Fails()
    {
        var saga = CreateSagaState();

        await saga.HandleAsync(new SendVerifyFailure(saga.SagaId, "the smtp server refused"));

        Assert.Equal(CreateStep.Failed, saga.Step);
        Assert.Equal("the smtp server refused", saga.FailureReason);
    }

    [Fact]
    public async Task CreateAccountFailure_FailsWithoutCompensation()
    {
        var bus = CreateBus();
        var saga = CreateSagaState();

        await saga.HandleAsync(new CreateAccountFailure(saga.SagaId, "the email is taken"));

        Assert.Equal(CreateStep.Failed, saga.Step);
        Assert.Equal("the email is taken", saga.FailureReason);
        bus.Verify(
            messageBus => messageBus.SendAsync(
                It.IsAny<SoftDeleteDoctorCommand>(),
                It.IsAny<DeliveryOptions>()
            ),
            Times.Never
        );
        bus.Verify(
            messageBus => messageBus.SendAsync(
                It.IsAny<SoftDeleteCommand>(),
                It.IsAny<DeliveryOptions>()
            ),
            Times.Never
        );
    }

    [Fact]
    public async Task SoftDeleteDoctorCompleted_SendsAccountSoftDelete()
    {
        var bus = CreateBus();
        var saga = CreateSagaState();

        await saga.HandleAsync(
            new SoftDeleteDoctorCompleted(saga.SagaId, saga.DoctorId),
            bus.Object
        );

        bus.Verify(
            messageBus => messageBus.SendAsync(
                It.Is<SoftDeleteCommand>(message => message.AccountId == saga.AccountId
                    && message.SagaId == saga.SagaId),
                It.IsAny<DeliveryOptions>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task SoftDeleteDoctorFailure_SendsAccountSoftDeleteAndAppendsReason()
    {
        var bus = CreateBus();
        var saga = CreateSagaState();
        saga.FailureReason = "the avatar file is missing";

        await saga.HandleAsync(
            new SoftDeleteDoctorFailure(saga.SagaId, "the profile is locked"),
            bus.Object
        );

        Assert.Equal(
            "the avatar file is missing (profile compensation failed: the profile is locked)",
            saga.FailureReason
        );
        bus.Verify(
            messageBus => messageBus.SendAsync(
                It.Is<SoftDeleteCommand>(message => message.AccountId == saga.AccountId),
                It.IsAny<DeliveryOptions>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task SoftDeleteCompleted_Fails()
    {
        var saga = CreateSagaState();
        saga.FailureReason = "the avatar file is missing";

        await saga.HandleAsync(new SoftDeleteCompleted(saga.SagaId, saga.AccountId));

        Assert.Equal(CreateStep.Failed, saga.Step);
        Assert.Equal("the avatar file is missing", saga.FailureReason);
    }

    [Fact]
    public async Task SoftDeleteFailure_FailsAndAppendsReason()
    {
        var saga = CreateSagaState();
        saga.FailureReason = "the avatar file is missing";

        await saga.HandleAsync(new SoftDeleteFailure(saga.SagaId, "the account is locked"));

        Assert.Equal(CreateStep.Failed, saga.Step);
        Assert.Contains("(account compensation failed: the account is locked)", saga.FailureReason);
    }

    [Fact]
    public async Task NotFound_SoftDeleteDoctorCompleted_DoesNothing()
    {
        var handled = CreateSaga.NotFound(new SoftDeleteDoctorCompleted(Guid.NewGuid(), Guid.NewGuid()));

        await handled;

        Assert.True(handled.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task NotFound_SoftDeleteDoctorFailure_DoesNothing()
    {
        var handled = CreateSaga.NotFound(new SoftDeleteDoctorFailure(Guid.NewGuid(), "boom"));

        await handled;

        Assert.True(handled.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task NotFound_SoftDeleteCompleted_DoesNothing()
    {
        var handled = CreateSaga.NotFound(new SoftDeleteCompleted(Guid.NewGuid(), Guid.NewGuid()));

        await handled;

        Assert.True(handled.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task NotFound_SoftDeleteFailure_DoesNothing()
    {
        var handled = CreateSaga.NotFound(new SoftDeleteFailure(Guid.NewGuid(), "boom"));

        await handled;

        Assert.True(handled.IsCompletedSuccessfully);
    }

    private static CreateSaga CreateSagaState() => new()
    {
        SagaId = Guid.CreateVersion7(),
        CreatorAccountId = Guid.CreateVersion7(),
        CreatorRole = RoleType.Admin,
        Role = RoleType.Doctor,
        Email = "doctor@example.test",
        AccountId = Guid.CreateVersion7(),
        DoctorId = Guid.CreateVersion7(),
    };

    private static DoctorProfileCreated CreateProfileCreatedEvent(CreateSaga saga) => new(
        saga.SagaId,
        saga.DoctorId,
        saga.AccountId,
        ProfileRole.Doctor
    );

    private static string CreateTempAvatar()
    {
        var path = Path.Combine(Path.GetTempPath(), $"avatar-{Guid.NewGuid():N}.png");
        File.WriteAllText(path, "png");
        return path;
    }

    private static void DeleteTempAvatar(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static Mock<IMediaClient> CreateMediaClient(Result<Guid> uploadResult)
    {
        var mediaClient = new Mock<IMediaClient>();
        mediaClient
            .Setup(client => client.UploadAvatarAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>()
            ))
            .ReturnsAsync(uploadResult);
        return mediaClient;
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
            It.IsAny<UpdateDoctorMediaIdCommand>(), It.IsAny<DeliveryOptions>()))
            .Returns(ValueTask.CompletedTask);
        bus.Setup(messageBus => messageBus.SendAsync(
            It.IsAny<SoftDeleteDoctorCommand>(), It.IsAny<DeliveryOptions>()))
            .Returns(ValueTask.CompletedTask);
        bus.Setup(messageBus => messageBus.SendAsync(
            It.IsAny<SoftDeleteCommand>(), It.IsAny<DeliveryOptions>()))
            .Returns(ValueTask.CompletedTask);
        return bus;
    }
}

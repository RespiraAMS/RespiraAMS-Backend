using Microsoft.Extensions.Logging;
using Moq;
using Respira.Saga.Application.Contracts.Media;
using Respira.Saga.Application.Contracts.Messages.Authentication.Update;
using Respira.Saga.Application.Contracts.Messages.Authentication.Update.Events;
using Respira.Saga.Application.Contracts.Messages.Doctor.Update;
using Respira.Saga.Application.Contracts.Messages.Doctor.Update.Events;
using Respira.Saga.Application.Features.Update.Commands;
using Respira.Saga.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Saga.Application.Test.Features.Update;

/// <summary>
/// Covers the media branches of the update saga: the avatar upload, the cleanup of the
/// replaced avatar and the terminal states of the flow. The update flow never
/// compensates: a failure goes straight to <see cref="UpdateStep.Failed"/>.
/// </summary>
public sealed class UpdateSagaFlowTest
{
    [Fact]
    public async Task UpdateAccountCompleted_WithoutTempAvatar_SendsProfileOnlyUpdate()
    {
        var bus = CreateBus();
        var mediaClient = new Mock<IMediaClient>();
        var saga = CreateSaga();
        saga.TempAvatarPath = null;

        await saga.HandleAsync(
            CreateAccountCompletedEvent(saga),
            bus.Object,
            mediaClient.Object,
            Mock.Of<ILogger<UpdateSaga>>(),
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
        Assert.Equal(UpdateStep.UpdatingProfile, saga.Step);
        bus.Verify(
            messageBus => messageBus.SendAsync(
                It.Is<UpdateDoctorProfileCommand>(message => message.NewMediaId == Guid.Empty
                    && message.DoctorId == saga.DoctorId
                    && message.AccountId == saga.AccountId),
                It.IsAny<DeliveryOptions>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task UpdateAccountCompleted_WithRealTempFile_UploadsAvatarAndLinksMedia()
    {
        var bus = CreateBus();
        var mediaId = Guid.NewGuid();
        var mediaClient = CreateMediaClient(Result<Guid>.Success(ApplicationStatus.Success, mediaId));
        var saga = CreateSaga();
        var path = CreateTempAvatar();
        saga.TempAvatarPath = path;

        try
        {
            await saga.HandleAsync(
                CreateAccountCompletedEvent(saga),
                bus.Object,
                mediaClient.Object,
                Mock.Of<ILogger<UpdateSaga>>(),
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
            Assert.Equal(UpdateStep.UpdatingProfile, saga.Step);
            bus.Verify(
                messageBus => messageBus.SendAsync(
                    It.Is<UpdateDoctorProfileCommand>(message => message.NewMediaId == mediaId),
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
    public async Task UpdateAccountCompleted_WithMissingTempFile_FailsWithoutUpload()
    {
        var bus = CreateBus();
        var mediaClient = new Mock<IMediaClient>();
        var saga = CreateSaga();
        saga.TempAvatarPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.png");

        await saga.HandleAsync(
            CreateAccountCompletedEvent(saga),
            bus.Object,
            mediaClient.Object,
            Mock.Of<ILogger<UpdateSaga>>(),
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
        Assert.Equal(UpdateStep.Failed, saga.Step);
        Assert.Equal("The avatar file is missing", saga.FailureReason);
        bus.Verify(
            messageBus => messageBus.SendAsync(
                It.IsAny<UpdateDoctorProfileCommand>(),
                It.IsAny<DeliveryOptions>()
            ),
            Times.Never
        );
    }

    [Fact]
    public async Task UpdateAccountCompleted_WhenUploadFails_FailsWithoutProfileUpdate()
    {
        var bus = CreateBus();
        var mediaClient = CreateMediaClient(
            Result<Guid>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "nope"))
        );
        var saga = CreateSaga();
        saga.TempAvatarPath = CreateTempAvatar();

        try
        {
            await saga.HandleAsync(
                CreateAccountCompletedEvent(saga),
                bus.Object,
                mediaClient.Object,
                Mock.Of<ILogger<UpdateSaga>>(),
                TestContext.Current.CancellationToken
            );

            Assert.Equal(UpdateStep.Failed, saga.Step);
            Assert.Equal("nope", saga.FailureReason);
            bus.Verify(
                messageBus => messageBus.SendAsync(
                    It.IsAny<UpdateDoctorProfileCommand>(),
                    It.IsAny<DeliveryOptions>()
                ),
                Times.Never
            );
        }
        finally
        {
            DeleteTempAvatar(saga.TempAvatarPath!);
        }
    }

    [Fact]
    public async Task UpdateAccountCompleted_WhenUploadThrows_DoesNotThrowAndFails()
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
        var saga = CreateSaga();
        saga.TempAvatarPath = CreateTempAvatar();

        try
        {
            await saga.HandleAsync(
                CreateAccountCompletedEvent(saga),
                bus.Object,
                mediaClient.Object,
                Mock.Of<ILogger<UpdateSaga>>(),
                TestContext.Current.CancellationToken
            );

            Assert.Equal(UpdateStep.Failed, saga.Step);
            Assert.Equal("the media service is down", saga.FailureReason);
        }
        finally
        {
            DeleteTempAvatar(saga.TempAvatarPath!);
        }
    }

    [Fact]
    public async Task UpdateAccountCompleted_WhenUploadReturnsEmptyGuid_Fails()
    {
        var bus = CreateBus();
        var mediaClient = CreateMediaClient(Result<Guid>.Success(ApplicationStatus.Success, Guid.Empty));
        var saga = CreateSaga();
        saga.TempAvatarPath = CreateTempAvatar();

        try
        {
            await saga.HandleAsync(
                CreateAccountCompletedEvent(saga),
                bus.Object,
                mediaClient.Object,
                Mock.Of<ILogger<UpdateSaga>>(),
                TestContext.Current.CancellationToken
            );

            Assert.Equal(UpdateStep.Failed, saga.Step);
            Assert.Equal("The avatar could not be uploaded", saga.FailureReason);
        }
        finally
        {
            DeleteTempAvatar(saga.TempAvatarPath!);
        }
    }

    [Fact]
    public async Task UpdateAccountCompleted_AfterSuccessfulUpload_DeletesTempAvatar()
    {
        var bus = CreateBus();
        var mediaClient = CreateMediaClient(Result<Guid>.Success(ApplicationStatus.Success, Guid.NewGuid()));
        var saga = CreateSaga();
        var path = CreateTempAvatar();
        saga.TempAvatarPath = path;

        try
        {
            await saga.HandleAsync(
                CreateAccountCompletedEvent(saga),
                bus.Object,
                mediaClient.Object,
                Mock.Of<ILogger<UpdateSaga>>(),
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
    public async Task UpdateAccountCompleted_AfterFailedUpload_DeletesTempAvatar()
    {
        var bus = CreateBus();
        var mediaClient = CreateMediaClient(
            Result<Guid>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "nope"))
        );
        var saga = CreateSaga();
        var path = CreateTempAvatar();
        saga.TempAvatarPath = path;

        try
        {
            await saga.HandleAsync(
                CreateAccountCompletedEvent(saga),
                bus.Object,
                mediaClient.Object,
                Mock.Of<ILogger<UpdateSaga>>(),
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
    public async Task UpdateAccountFailure_FailsWithoutProfileUpdate()
    {
        var bus = CreateBus();
        var saga = CreateSaga();

        await saga.HandleAsync(
            new UpdateAccountFailure
            {
                SagaId = saga.SagaId,
                AccountId = saga.AccountId,
                Reason = "the email is taken",
            }
        );

        Assert.Equal(UpdateStep.Failed, saga.Step);
        Assert.Equal("the email is taken", saga.FailureReason);
        bus.Verify(
            messageBus => messageBus.SendAsync(
                It.IsAny<UpdateDoctorProfileCommand>(),
                It.IsAny<DeliveryOptions>()
            ),
            Times.Never
        );
    }

    [Fact]
    public async Task DoctorProfileUpdated_WithoutOldMedia_DoesNotDeleteMedia()
    {
        var mediaClient = new Mock<IMediaClient>();
        var saga = CreateSaga();
        var mediaId = Guid.NewGuid();

        await saga.HandleAsync(
            new DoctorProfileUpdated(saga.SagaId, saga.DoctorId, mediaId, Guid.Empty),
            mediaClient.Object,
            Mock.Of<ILogger<UpdateSaga>>(),
            TestContext.Current.CancellationToken
        );

        mediaClient.Verify(
            client => client.DeleteAssetAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>()
            ),
            Times.Never
        );
        Assert.Equal(mediaId, saga.MediaId);
        Assert.Equal(UpdateStep.Completed, saga.Step);
    }

    [Fact]
    public async Task DoctorProfileUpdated_WithSameMediaId_DoesNotDeleteMedia()
    {
        var mediaClient = new Mock<IMediaClient>();
        var saga = CreateSaga();
        var mediaId = Guid.NewGuid();

        await saga.HandleAsync(
            new DoctorProfileUpdated(saga.SagaId, saga.DoctorId, mediaId, mediaId),
            mediaClient.Object,
            Mock.Of<ILogger<UpdateSaga>>(),
            TestContext.Current.CancellationToken
        );

        mediaClient.Verify(
            client => client.DeleteAssetAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>()
            ),
            Times.Never
        );
        Assert.Equal(UpdateStep.Completed, saga.Step);
    }

    [Fact]
    public async Task DoctorProfileUpdated_WithDifferentOldMedia_DeletesOldMedia()
    {
        var mediaClient = new Mock<IMediaClient>();
        mediaClient
            .Setup(client => client.DeleteAssetAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>()
            ))
            .ReturnsAsync(Result<Guid>.Success(ApplicationStatus.Success, Guid.NewGuid()));
        var saga = CreateSaga();
        var mediaId = Guid.NewGuid();
        var oldMediaId = Guid.NewGuid();

        await saga.HandleAsync(
            new DoctorProfileUpdated(saga.SagaId, saga.DoctorId, mediaId, oldMediaId),
            mediaClient.Object,
            Mock.Of<ILogger<UpdateSaga>>(),
            TestContext.Current.CancellationToken
        );

        mediaClient.Verify(
            client => client.DeleteAssetAsync(
                oldMediaId,
                It.IsAny<CancellationToken>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>()
            ),
            Times.Once
        );
        Assert.Equal(UpdateStep.Completed, saga.Step);
    }

    [Fact]
    public async Task DoctorProfileUpdated_WhenDeletionFails_Throws()
    {
        var mediaClient = new Mock<IMediaClient>();
        mediaClient
            .Setup(client => client.DeleteAssetAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<Guid?>(),
                It.IsAny<string>()
            ))
            .ReturnsAsync(Result<Guid>.Failure(new ApplicationError(ApplicationStatus.BadRequest, "nope")));
        var saga = CreateSaga();
        var oldMediaId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => saga.HandleAsync(
                new DoctorProfileUpdated(saga.SagaId, saga.DoctorId, Guid.NewGuid(), oldMediaId),
                mediaClient.Object,
                Mock.Of<ILogger<UpdateSaga>>(),
                TestContext.Current.CancellationToken
            )
        );

        Assert.Contains("Could not delete replaced avatar", exception.Message);
        Assert.Contains("nope", exception.Message);
    }

    [Fact]
    public async Task DoctorProfileUpdated_WhenDeletionThrows_Throws()
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
        var oldMediaId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => saga.HandleAsync(
                new DoctorProfileUpdated(saga.SagaId, saga.DoctorId, Guid.NewGuid(), oldMediaId),
                mediaClient.Object,
                Mock.Of<ILogger<UpdateSaga>>(),
                TestContext.Current.CancellationToken
            )
        );

        Assert.Contains("Could not delete replaced avatar", exception.Message);
        Assert.Contains("the media service is down", exception.Message);
    }

    [Fact]
    public async Task DoctorProfileUpdateFailure_Fails()
    {
        var saga = CreateSaga();

        await saga.HandleAsync(
            new DoctorProfileUpdateFailure(saga.SagaId, "the profile was rejected")
        );

        Assert.Equal(UpdateStep.Failed, saga.Step);
        Assert.Equal("the profile was rejected", saga.FailureReason);
    }

    [Fact]
    public async Task StartAsync_WithNullPassword_KeepsTheCurrentPassword()
    {
        var bus = CreateBus();
        var saga = new UpdateSaga();
        var command = new StartUpdateCommand
        {
            SagaId = Guid.CreateVersion7(),
            ActorAccountId = Guid.CreateVersion7(),
            ActorRole = "Admin",
            AccountId = Guid.CreateVersion7(),
            DoctorId = Guid.CreateVersion7(),
            Email = "doctor@example.test",
            Phone = "0900000000",
            Password = null,
        };

        await saga.StartAsync(command, bus.Object);

        bus.Verify(messageBus => messageBus.SendAsync(
            It.Is<UpdateAccountCommand>(message => message.Password == null
                && message.AccountId == command.AccountId),
            It.IsAny<DeliveryOptions>()
        ), Times.Once);
    }

    [Fact]
    public async Task StartAsync_WithPassword_DoesNotKeepThePasswordInTheState()
    {
        var bus = CreateBus();
        var saga = new UpdateSaga();
        var command = new StartUpdateCommand
        {
            SagaId = Guid.CreateVersion7(),
            ActorAccountId = Guid.CreateVersion7(),
            ActorRole = "Admin",
            AccountId = Guid.CreateVersion7(),
            DoctorId = Guid.CreateVersion7(),
            Email = "doctor@example.test",
            Phone = "0900000000",
            Password = "Replacement-123",
        };

        await saga.StartAsync(command, bus.Object);

        bus.Verify(messageBus => messageBus.SendAsync(
            It.Is<UpdateAccountCommand>(message => message.Password == "Replacement-123"),
            It.IsAny<DeliveryOptions>()
        ), Times.Once);
        Assert.Null(typeof(UpdateSaga).GetProperty(nameof(command.Password)));
    }

    [Fact]
    public async Task StartAsync_NormalizesTheEmail()
    {
        var bus = CreateBus();
        var saga = new UpdateSaga();
        var command = new StartUpdateCommand
        {
            SagaId = Guid.CreateVersion7(),
            ActorAccountId = Guid.CreateVersion7(),
            ActorRole = "Admin",
            AccountId = Guid.CreateVersion7(),
            DoctorId = Guid.CreateVersion7(),
            Email = "  Doctor@Example.test ",
            Phone = "0900000000",
        };

        await saga.StartAsync(command, bus.Object);

        Assert.Equal("doctor@example.test", saga.Email);
        bus.Verify(messageBus => messageBus.SendAsync(
            It.Is<UpdateAccountCommand>(message => message.Email == "doctor@example.test"),
            It.IsAny<DeliveryOptions>()
        ), Times.Once);
    }

    [Fact]
    public async Task StartAsync_CopiesTheTargetsIntoTheState()
    {
        var bus = CreateBus();
        var saga = new UpdateSaga();
        var command = new StartUpdateCommand
        {
            SagaId = Guid.CreateVersion7(),
            ActorAccountId = Guid.CreateVersion7(),
            ActorRole = "HeadDepartmentDoctor",
            AccountId = Guid.CreateVersion7(),
            DoctorId = Guid.CreateVersion7(),
            Email = "doctor@example.test",
            Phone = "0900000000",
            FirstName = "Ana",
            LastName = "Souza",
            TempAvatarPath = "/tmp/avatar.png",
        };

        await saga.StartAsync(command, bus.Object);

        Assert.Equal(command.SagaId, saga.SagaId);
        Assert.Equal(command.AccountId, saga.AccountId);
        Assert.Equal(command.DoctorId, saga.DoctorId);
        Assert.Equal(command.ActorAccountId, saga.ActorAccountId);
        Assert.Equal("HeadDepartmentDoctor", saga.ActorRole);
        Assert.Equal("0900000000", saga.Phone);
        Assert.Equal("Ana", saga.FirstName);
        Assert.Equal("Souza", saga.LastName);
        Assert.Equal("/tmp/avatar.png", saga.TempAvatarPath);
        Assert.Equal(UpdateStep.UpdatingAccount, saga.Step);
    }

    private static UpdateSaga CreateSaga() => new()
    {
        SagaId = Guid.CreateVersion7(),
        ActorAccountId = Guid.CreateVersion7(),
        ActorRole = "Admin",
        AccountId = Guid.CreateVersion7(),
        DoctorId = Guid.CreateVersion7(),
        Email = "doctor@example.test",
        Phone = "0900000000",
    };

    private static UpdateAccountCompleted CreateAccountCompletedEvent(UpdateSaga saga) => new()
    {
        SagaId = saga.SagaId,
        AccountId = saga.AccountId,
        OldEmail = "old@example.test",
        OldPassword = "old-password",
        OldPhone = "0900000000",
    };

    private static string CreateTempAvatar()
    {
        var path = Path.Combine(Path.GetTempPath(), $"avatar-{Guid.NewGuid():N}.png");
        File.WriteAllText(path, "png");
        return path;
    }

    private static void DeleteTempAvatar(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
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
            It.IsAny<UpdateAccountCommand>(), It.IsAny<DeliveryOptions>()))
            .Returns(ValueTask.CompletedTask);
        bus.Setup(messageBus => messageBus.SendAsync(
            It.IsAny<UpdateDoctorProfileCommand>(), It.IsAny<DeliveryOptions>()))
            .Returns(ValueTask.CompletedTask);
        return bus;
    }
}

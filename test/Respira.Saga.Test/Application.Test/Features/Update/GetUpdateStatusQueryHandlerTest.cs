using Moq;
using Respira.Saga.Application.Contracts.Sagas;
using Respira.Saga.Application.Features.Update.Commands;
using Respira.Saga.Application.Features.Update.Queries;
using Respira.Saga.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Saga.Application.Test.Features.Update.Queries;

/// <summary>
/// Covers the read side of the profile update flow: the snapshot of a run that is still
/// in flight and the "not found" answer for a run that already finished.
/// </summary>
public sealed class GetUpdateStatusQueryHandlerTest
{
    private readonly Mock<ISagaStateReader> _sagaStates = new();

    [Fact]
    public async Task HandleAsync_WhenTheSagaDoesNotExist_ReturnsResourceNotFound()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<UpdateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UpdateSaga?)null);
        var handler = new GetUpdateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetUpdateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(ApplicationStatus.ResourceNotFound, result.Error.StatusCode);
        Assert.Equal(
            "No profile update with this id is currently in progress; it has either finished or never started",
            result.Error.Description
        );
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task HandleAsync_WhenTheSagaExists_MirrorsItsState()
    {
        var sagaId = Guid.CreateVersion7();
        var accountId = Guid.CreateVersion7();
        var doctorId = Guid.CreateVersion7();
        var mediaId = Guid.CreateVersion7();
        _sagaStates
            .Setup(reader => reader.ReadAsync<UpdateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateSaga
            {
                SagaId = sagaId,
                Step = UpdateStep.UpdatingProfile,
                AccountId = accountId,
                DoctorId = doctorId,
                MediaId = mediaId,
                FailureReason = "the avatar file is missing",
            });
        var handler = new GetUpdateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetUpdateStatusQuery { SagaId = sagaId },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(sagaId, data.SagaId);
        Assert.Equal(UpdateStep.UpdatingProfile, data.Step);
        Assert.Equal(accountId, data.AccountId);
        Assert.Equal(doctorId, data.DoctorId);
        Assert.Equal(mediaId, data.MediaId);
        Assert.Equal("the avatar file is missing", data.FailureReason);
    }

    [Theory]
    [InlineData(UpdateStep.UpdatingAccount)]
    [InlineData(UpdateStep.UploadingAvatar)]
    [InlineData(UpdateStep.UpdatingProfile)]
    [InlineData(UpdateStep.DeletingOldMedia)]
    [InlineData(UpdateStep.Completed)]
    [InlineData(UpdateStep.Failed)]
    public async Task HandleAsync_MirrorsEveryStep(UpdateStep step)
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<UpdateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateSaga { SagaId = Guid.CreateVersion7(), Step = step });
        var handler = new GetUpdateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetUpdateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(step, data.Step);
    }

    [Fact]
    public async Task HandleAsync_WithoutAFailureReason_KeepsItNull()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<UpdateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateSaga
            {
                SagaId = Guid.CreateVersion7(),
                Step = UpdateStep.UpdatingAccount,
            });
        var handler = new GetUpdateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetUpdateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Null(data.FailureReason);
    }

    [Fact]
    public async Task HandleAsync_WithANullQuery_Throws()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<UpdateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UpdateSaga?)null);
        var handler = new GetUpdateStatusQueryHandler(_sagaStates.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task HandleAsync_AsksTheReaderForTheExactSagaId()
    {
        var sagaId = Guid.CreateVersion7();
        _sagaStates
            .Setup(reader => reader.ReadAsync<UpdateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UpdateSaga?)null);
        var handler = new GetUpdateStatusQueryHandler(_sagaStates.Object);

        await handler.HandleAsync(
            new GetUpdateStatusQuery { SagaId = sagaId },
            TestContext.Current.CancellationToken
        );

        _sagaStates.Verify(
            reader => reader.ReadAsync<UpdateSaga>(sagaId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_ReportsTheSagaIdOfTheSagaThatWasRead()
    {
        var sagaId = Guid.CreateVersion7();
        _sagaStates
            .Setup(reader => reader.ReadAsync<UpdateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateSaga { SagaId = sagaId, Step = UpdateStep.UpdatingAccount });
        var handler = new GetUpdateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetUpdateStatusQuery { SagaId = sagaId },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(sagaId, data.SagaId);
    }

    [Fact]
    public async Task HandleAsync_WithAnUpdatingAccountSaga_ReportsUpdatingAccount()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<UpdateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateSaga { SagaId = Guid.CreateVersion7(), Step = UpdateStep.UpdatingAccount });
        var handler = new GetUpdateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetUpdateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(UpdateStep.UpdatingAccount, data.Step);
    }

    [Fact]
    public async Task HandleAsync_WithAnUploadingAvatarSaga_ReportsUploadingAvatar()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<UpdateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateSaga { SagaId = Guid.CreateVersion7(), Step = UpdateStep.UploadingAvatar });
        var handler = new GetUpdateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetUpdateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(UpdateStep.UploadingAvatar, data.Step);
    }

    [Fact]
    public async Task HandleAsync_WithAnUpdatingProfileSaga_ReportsUpdatingProfile()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<UpdateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateSaga { SagaId = Guid.CreateVersion7(), Step = UpdateStep.UpdatingProfile });
        var handler = new GetUpdateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetUpdateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(UpdateStep.UpdatingProfile, data.Step);
    }

    [Fact]
    public async Task HandleAsync_WithADeletingOldMediaSaga_ReportsDeletingOldMedia()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<UpdateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateSaga { SagaId = Guid.CreateVersion7(), Step = UpdateStep.DeletingOldMedia });
        var handler = new GetUpdateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetUpdateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(UpdateStep.DeletingOldMedia, data.Step);
    }

    [Fact]
    public async Task HandleAsync_WithACompletedSaga_ReportsCompleted()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<UpdateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateSaga { SagaId = Guid.CreateVersion7(), Step = UpdateStep.Completed });
        var handler = new GetUpdateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetUpdateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(UpdateStep.Completed, data.Step);
    }

    [Fact]
    public async Task HandleAsync_WithAFailedSaga_ReportsFailed()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<UpdateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateSaga { SagaId = Guid.CreateVersion7(), Step = UpdateStep.Failed });
        var handler = new GetUpdateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetUpdateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(UpdateStep.Failed, data.Step);
    }

    [Fact]
    public async Task HandleAsync_MirrorsTheAccountId()
    {
        var accountId = Guid.CreateVersion7();
        _sagaStates
            .Setup(reader => reader.ReadAsync<UpdateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateSaga { SagaId = Guid.CreateVersion7(), AccountId = accountId });
        var handler = new GetUpdateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetUpdateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(accountId, data.AccountId);
    }

    [Fact]
    public async Task HandleAsync_MirrorsTheDoctorId()
    {
        var doctorId = Guid.CreateVersion7();
        _sagaStates
            .Setup(reader => reader.ReadAsync<UpdateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateSaga { SagaId = Guid.CreateVersion7(), DoctorId = doctorId });
        var handler = new GetUpdateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetUpdateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(doctorId, data.DoctorId);
    }

    [Fact]
    public async Task HandleAsync_MirrorsTheMediaId()
    {
        var mediaId = Guid.CreateVersion7();
        _sagaStates
            .Setup(reader => reader.ReadAsync<UpdateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateSaga { SagaId = Guid.CreateVersion7(), MediaId = mediaId });
        var handler = new GetUpdateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetUpdateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(mediaId, data.MediaId);
    }

    [Fact]
    public async Task HandleAsync_WithAFailureReason_ReportsThatReason()
    {
        const string reason = "the new avatar could not be uploaded";
        _sagaStates
            .Setup(reader => reader.ReadAsync<UpdateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateSaga
            {
                SagaId = Guid.CreateVersion7(),
                Step = UpdateStep.Failed,
                FailureReason = reason,
            });
        var handler = new GetUpdateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetUpdateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(reason, data.FailureReason);
    }

    [Fact]
    public async Task HandleAsync_AFailedSagaWithoutAFailureReason_ReportsANullReason()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<UpdateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateSaga
            {
                SagaId = Guid.CreateVersion7(),
                Step = UpdateStep.Failed,
                FailureReason = null,
            });
        var handler = new GetUpdateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetUpdateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(UpdateStep.Failed, data.Step);
        Assert.Null(data.FailureReason);
    }

    [Fact]
    public async Task HandleAsync_WithASagaIdDifferentFromTheQuery_ReportsTheSagaOwnId()
    {
        var sagaId = Guid.CreateVersion7();
        _sagaStates
            .Setup(reader => reader.ReadAsync<UpdateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateSaga { SagaId = sagaId, Step = UpdateStep.UploadingAvatar });
        var handler = new GetUpdateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetUpdateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(sagaId, data.SagaId);
    }

    [Fact]
    public async Task HandleAsync_ReadsTheSagaExactlyOnce()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<UpdateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateSaga { SagaId = Guid.CreateVersion7() });
        var handler = new GetUpdateStatusQueryHandler(_sagaStates.Object);

        await handler.HandleAsync(
            new GetUpdateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        _sagaStates.Verify(
            reader => reader.ReadAsync<UpdateSaga>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_PassesTheCancellationTokenToTheReader()
    {
        var expectedToken = TestContext.Current.CancellationToken;
        CancellationToken observedToken = default;
        _sagaStates
            .Setup(reader => reader.ReadAsync<UpdateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((_, token) => observedToken = token)
            .ReturnsAsync(new UpdateSaga { SagaId = Guid.CreateVersion7() });
        var handler = new GetUpdateStatusQueryHandler(_sagaStates.Object);

        await handler.HandleAsync(
            new GetUpdateStatusQuery { SagaId = Guid.CreateVersion7() },
            expectedToken
        );

        Assert.Equal(expectedToken, observedToken);
    }

    [Fact]
    public async Task HandleAsync_AsksTheReaderForTheUpdateSagaType()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<UpdateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateSaga { SagaId = Guid.CreateVersion7() });
        var handler = new GetUpdateStatusQueryHandler(_sagaStates.Object);

        await handler.HandleAsync(
            new GetUpdateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        _sagaStates.Verify(
            reader => reader.ReadAsync<UpdateSaga>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}

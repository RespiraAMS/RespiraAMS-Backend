using Moq;
using Respira.Saga.Application.Contracts.Sagas;
using Respira.Saga.Application.Features.Delete.Commands;
using Respira.Saga.Application.Features.Delete.Queries;
using Respira.Saga.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Saga.Application.Test.Features.Delete.Queries;

/// <summary>
/// Covers the read side of the profile deletion flow: the snapshot of a run that is still
/// in flight and the "not found" answer for a run that already finished.
/// </summary>
public sealed class GetDeleteStatusQueryHandlerTest
{
    private readonly Mock<ISagaStateReader> _sagaStates = new();

    [Fact]
    public async Task HandleAsync_WhenTheSagaDoesNotExist_ReturnsResourceNotFound()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<DeleteSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeleteSaga?)null);
        var handler = new GetDeleteStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetDeleteStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(ApplicationStatus.ResourceNotFound, result.Error.StatusCode);
        Assert.Equal(
            "No profile deletion with this id is currently in progress; it has either finished or never started",
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
            .Setup(reader => reader.ReadAsync<DeleteSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteSaga
            {
                SagaId = sagaId,
                Step = DeleteStep.DeletingAccount,
                AccountId = accountId,
                DoctorId = doctorId,
                MediaId = mediaId,
                FailureReason = "the profile is locked",
            });
        var handler = new GetDeleteStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetDeleteStatusQuery { SagaId = sagaId },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(sagaId, data.SagaId);
        Assert.Equal(DeleteStep.DeletingAccount, data.Step);
        Assert.Equal(accountId, data.AccountId);
        Assert.Equal(doctorId, data.DoctorId);
        Assert.Equal(mediaId, data.MediaId);
        Assert.Equal("the profile is locked", data.FailureReason);
    }

    [Theory]
    [InlineData(DeleteStep.DeletingProfile)]
    [InlineData(DeleteStep.DeletingAccount)]
    [InlineData(DeleteStep.DeletingMedia)]
    [InlineData(DeleteStep.Completed)]
    [InlineData(DeleteStep.Failed)]
    public async Task HandleAsync_MirrorsEveryStep(DeleteStep step)
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<DeleteSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteSaga { SagaId = Guid.CreateVersion7(), Step = step });
        var handler = new GetDeleteStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetDeleteStatusQuery { SagaId = Guid.CreateVersion7() },
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
            .Setup(reader => reader.ReadAsync<DeleteSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteSaga
            {
                SagaId = Guid.CreateVersion7(),
                Step = DeleteStep.DeletingProfile,
            });
        var handler = new GetDeleteStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetDeleteStatusQuery { SagaId = Guid.CreateVersion7() },
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
            .Setup(reader => reader.ReadAsync<DeleteSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeleteSaga?)null);
        var handler = new GetDeleteStatusQueryHandler(_sagaStates.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task HandleAsync_AsksTheReaderForTheExactSagaId()
    {
        var sagaId = Guid.CreateVersion7();
        _sagaStates
            .Setup(reader => reader.ReadAsync<DeleteSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeleteSaga?)null);
        var handler = new GetDeleteStatusQueryHandler(_sagaStates.Object);

        await handler.HandleAsync(
            new GetDeleteStatusQuery { SagaId = sagaId },
            TestContext.Current.CancellationToken
        );

        _sagaStates.Verify(
            reader => reader.ReadAsync<DeleteSaga>(sagaId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_ReportsTheSagaIdOfTheSagaThatWasRead()
    {
        var sagaId = Guid.CreateVersion7();
        _sagaStates
            .Setup(reader => reader.ReadAsync<DeleteSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteSaga { SagaId = sagaId, Step = DeleteStep.DeletingProfile });
        var handler = new GetDeleteStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetDeleteStatusQuery { SagaId = sagaId },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(sagaId, data.SagaId);
    }

    [Fact]
    public async Task HandleAsync_WithADeletingProfileSaga_ReportsDeletingProfile()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<DeleteSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteSaga { SagaId = Guid.CreateVersion7(), Step = DeleteStep.DeletingProfile });
        var handler = new GetDeleteStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetDeleteStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(DeleteStep.DeletingProfile, data.Step);
    }

    [Fact]
    public async Task HandleAsync_WithADeletingAccountSaga_ReportsDeletingAccount()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<DeleteSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteSaga { SagaId = Guid.CreateVersion7(), Step = DeleteStep.DeletingAccount });
        var handler = new GetDeleteStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetDeleteStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(DeleteStep.DeletingAccount, data.Step);
    }

    [Fact]
    public async Task HandleAsync_WithADeletingMediaSaga_ReportsDeletingMedia()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<DeleteSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteSaga { SagaId = Guid.CreateVersion7(), Step = DeleteStep.DeletingMedia });
        var handler = new GetDeleteStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetDeleteStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(DeleteStep.DeletingMedia, data.Step);
    }

    [Fact]
    public async Task HandleAsync_WithACompletedSaga_ReportsCompleted()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<DeleteSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteSaga { SagaId = Guid.CreateVersion7(), Step = DeleteStep.Completed });
        var handler = new GetDeleteStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetDeleteStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(DeleteStep.Completed, data.Step);
    }

    [Fact]
    public async Task HandleAsync_WithAFailedSaga_ReportsFailed()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<DeleteSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteSaga { SagaId = Guid.CreateVersion7(), Step = DeleteStep.Failed });
        var handler = new GetDeleteStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetDeleteStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(DeleteStep.Failed, data.Step);
    }

    [Fact]
    public async Task HandleAsync_MirrorsTheAccountId()
    {
        var accountId = Guid.CreateVersion7();
        _sagaStates
            .Setup(reader => reader.ReadAsync<DeleteSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteSaga { SagaId = Guid.CreateVersion7(), AccountId = accountId });
        var handler = new GetDeleteStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetDeleteStatusQuery { SagaId = Guid.CreateVersion7() },
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
            .Setup(reader => reader.ReadAsync<DeleteSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteSaga { SagaId = Guid.CreateVersion7(), DoctorId = doctorId });
        var handler = new GetDeleteStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetDeleteStatusQuery { SagaId = Guid.CreateVersion7() },
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
            .Setup(reader => reader.ReadAsync<DeleteSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteSaga { SagaId = Guid.CreateVersion7(), MediaId = mediaId });
        var handler = new GetDeleteStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetDeleteStatusQuery { SagaId = Guid.CreateVersion7() },
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
        const string reason = "the media service is unreachable";
        _sagaStates
            .Setup(reader => reader.ReadAsync<DeleteSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteSaga
            {
                SagaId = Guid.CreateVersion7(),
                Step = DeleteStep.Failed,
                FailureReason = reason,
            });
        var handler = new GetDeleteStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetDeleteStatusQuery { SagaId = Guid.CreateVersion7() },
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
            .Setup(reader => reader.ReadAsync<DeleteSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteSaga
            {
                SagaId = Guid.CreateVersion7(),
                Step = DeleteStep.Failed,
                FailureReason = null,
            });
        var handler = new GetDeleteStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetDeleteStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(DeleteStep.Failed, data.Step);
        Assert.Null(data.FailureReason);
    }

    [Fact]
    public async Task HandleAsync_WithASagaIdDifferentFromTheQuery_ReportsTheSagaOwnId()
    {
        var sagaId = Guid.CreateVersion7();
        _sagaStates
            .Setup(reader => reader.ReadAsync<DeleteSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteSaga { SagaId = sagaId, Step = DeleteStep.DeletingMedia });
        var handler = new GetDeleteStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetDeleteStatusQuery { SagaId = Guid.CreateVersion7() },
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
            .Setup(reader => reader.ReadAsync<DeleteSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteSaga { SagaId = Guid.CreateVersion7() });
        var handler = new GetDeleteStatusQueryHandler(_sagaStates.Object);

        await handler.HandleAsync(
            new GetDeleteStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        _sagaStates.Verify(
            reader => reader.ReadAsync<DeleteSaga>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_PassesTheCancellationTokenToTheReader()
    {
        var expectedToken = TestContext.Current.CancellationToken;
        CancellationToken observedToken = default;
        _sagaStates
            .Setup(reader => reader.ReadAsync<DeleteSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((_, token) => observedToken = token)
            .ReturnsAsync(new DeleteSaga { SagaId = Guid.CreateVersion7() });
        var handler = new GetDeleteStatusQueryHandler(_sagaStates.Object);

        await handler.HandleAsync(
            new GetDeleteStatusQuery { SagaId = Guid.CreateVersion7() },
            expectedToken
        );

        Assert.Equal(expectedToken, observedToken);
    }

    [Fact]
    public async Task HandleAsync_AsksTheReaderForTheDeleteSagaType()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<DeleteSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteSaga { SagaId = Guid.CreateVersion7() });
        var handler = new GetDeleteStatusQueryHandler(_sagaStates.Object);

        await handler.HandleAsync(
            new GetDeleteStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        _sagaStates.Verify(
            reader => reader.ReadAsync<DeleteSaga>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}

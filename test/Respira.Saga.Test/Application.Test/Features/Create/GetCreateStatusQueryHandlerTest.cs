using Moq;
using Respira.Saga.Application.Contracts.Sagas;
using Respira.Saga.Application.Features.Create.Commands;
using Respira.Saga.Application.Features.Create.Queries;
using Respira.Saga.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Saga.Application.Test.Features.Create.Queries;

/// <summary>
/// Covers the read side of the profile creation flow: the snapshot of a run that is still
/// in flight and the "not found" answer for a run that already finished.
/// </summary>
public sealed class GetCreateStatusQueryHandlerTest
{
    private readonly Mock<ISagaStateReader> _sagaStates = new();

    [Fact]
    public async Task HandleAsync_WhenTheSagaDoesNotExist_ReturnsResourceNotFound()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<CreateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateSaga?)null);
        var handler = new GetCreateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetCreateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.NotNull(result.Error);
        Assert.Equal(ApplicationStatus.ResourceNotFound, result.Error.StatusCode);
        Assert.Equal(
            "No profile creation with this id is currently in progress; it has either finished or never started",
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
            .Setup(reader => reader.ReadAsync<CreateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateSaga
            {
                SagaId = sagaId,
                Step = CreateStep.LinkingMedia,
                AccountId = accountId,
                DoctorId = doctorId,
                MediaId = mediaId,
                FailureReason = "the avatar could not be uploaded",
            });
        var handler = new GetCreateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetCreateStatusQuery { SagaId = sagaId },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(sagaId, data.SagaId);
        Assert.Equal(CreateStep.LinkingMedia, data.Step);
        Assert.Equal(accountId, data.AccountId);
        Assert.Equal(doctorId, data.DoctorId);
        Assert.Equal(mediaId, data.MediaId);
        Assert.Equal("the avatar could not be uploaded", data.FailureReason);
    }

    [Theory]
    [InlineData(CreateStep.CreatingAccount)]
    [InlineData(CreateStep.CreatingProfile)]
    [InlineData(CreateStep.CreatingMedia)]
    [InlineData(CreateStep.LinkingMedia)]
    [InlineData(CreateStep.SendingVerificationEmail)]
    [InlineData(CreateStep.Compensating)]
    [InlineData(CreateStep.Completed)]
    [InlineData(CreateStep.Failed)]
    public async Task HandleAsync_MirrorsEveryStep(CreateStep step)
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<CreateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateSaga { SagaId = Guid.CreateVersion7(), Step = step });
        var handler = new GetCreateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetCreateStatusQuery { SagaId = Guid.CreateVersion7() },
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
            .Setup(reader => reader.ReadAsync<CreateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateSaga
            {
                SagaId = Guid.CreateVersion7(),
                Step = CreateStep.CreatingAccount,
            });
        var handler = new GetCreateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetCreateStatusQuery { SagaId = Guid.CreateVersion7() },
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
            .Setup(reader => reader.ReadAsync<CreateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateSaga?)null);
        var handler = new GetCreateStatusQueryHandler(_sagaStates.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task HandleAsync_AsksTheReaderForTheExactSagaId()
    {
        var sagaId = Guid.CreateVersion7();
        _sagaStates
            .Setup(reader => reader.ReadAsync<CreateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateSaga?)null);
        var handler = new GetCreateStatusQueryHandler(_sagaStates.Object);

        await handler.HandleAsync(
            new GetCreateStatusQuery { SagaId = sagaId },
            TestContext.Current.CancellationToken
        );

        _sagaStates.Verify(
            reader => reader.ReadAsync<CreateSaga>(sagaId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_ReportsTheSagaIdOfTheSagaThatWasRead()
    {
        var sagaId = Guid.CreateVersion7();
        _sagaStates
            .Setup(reader => reader.ReadAsync<CreateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateSaga { SagaId = sagaId, Step = CreateStep.CreatingAccount });
        var handler = new GetCreateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetCreateStatusQuery { SagaId = sagaId },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(sagaId, data.SagaId);
    }

    [Fact]
    public async Task HandleAsync_WithACompensatingSaga_ReportsCompensating()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<CreateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateSaga { SagaId = Guid.CreateVersion7(), Step = CreateStep.Compensating });
        var handler = new GetCreateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetCreateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(CreateStep.Compensating, data.Step);
    }

    [Fact]
    public async Task HandleAsync_WithACompletedSaga_ReportsCompleted()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<CreateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateSaga { SagaId = Guid.CreateVersion7(), Step = CreateStep.Completed });
        var handler = new GetCreateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetCreateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(CreateStep.Completed, data.Step);
    }

    [Fact]
    public async Task HandleAsync_WithAFailedSaga_ReportsFailed()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<CreateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateSaga { SagaId = Guid.CreateVersion7(), Step = CreateStep.Failed });
        var handler = new GetCreateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetCreateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(CreateStep.Failed, data.Step);
    }

    [Fact]
    public async Task HandleAsync_MirrorsTheAccountId()
    {
        var accountId = Guid.CreateVersion7();
        _sagaStates
            .Setup(reader => reader.ReadAsync<CreateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateSaga { SagaId = Guid.CreateVersion7(), AccountId = accountId });
        var handler = new GetCreateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetCreateStatusQuery { SagaId = Guid.CreateVersion7() },
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
            .Setup(reader => reader.ReadAsync<CreateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateSaga { SagaId = Guid.CreateVersion7(), DoctorId = doctorId });
        var handler = new GetCreateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetCreateStatusQuery { SagaId = Guid.CreateVersion7() },
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
            .Setup(reader => reader.ReadAsync<CreateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateSaga { SagaId = Guid.CreateVersion7(), MediaId = mediaId });
        var handler = new GetCreateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetCreateStatusQuery { SagaId = Guid.CreateVersion7() },
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
        const string reason = "the verification email could not be sent";
        _sagaStates
            .Setup(reader => reader.ReadAsync<CreateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateSaga
            {
                SagaId = Guid.CreateVersion7(),
                Step = CreateStep.Failed,
                FailureReason = reason,
            });
        var handler = new GetCreateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetCreateStatusQuery { SagaId = Guid.CreateVersion7() },
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
            .Setup(reader => reader.ReadAsync<CreateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateSaga
            {
                SagaId = Guid.CreateVersion7(),
                Step = CreateStep.Failed,
                FailureReason = null,
            });
        var handler = new GetCreateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetCreateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        var data = result.Data;
        Assert.NotNull(data);
        Assert.Equal(CreateStep.Failed, data.Step);
        Assert.Null(data.FailureReason);
    }

    [Fact]
    public async Task HandleAsync_WithASagaIdDifferentFromTheQuery_ReportsTheSagaOwnId()
    {
        var sagaId = Guid.CreateVersion7();
        _sagaStates
            .Setup(reader => reader.ReadAsync<CreateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateSaga { SagaId = sagaId, Step = CreateStep.CreatingMedia });
        var handler = new GetCreateStatusQueryHandler(_sagaStates.Object);

        var result = await handler.HandleAsync(
            new GetCreateStatusQuery { SagaId = Guid.CreateVersion7() },
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
            .Setup(reader => reader.ReadAsync<CreateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateSaga { SagaId = Guid.CreateVersion7() });
        var handler = new GetCreateStatusQueryHandler(_sagaStates.Object);

        await handler.HandleAsync(
            new GetCreateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        _sagaStates.Verify(
            reader => reader.ReadAsync<CreateSaga>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_PassesTheCancellationTokenToTheReader()
    {
        var expectedToken = TestContext.Current.CancellationToken;
        CancellationToken observedToken = default;
        _sagaStates
            .Setup(reader => reader.ReadAsync<CreateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid, CancellationToken>((_, token) => observedToken = token)
            .ReturnsAsync(new CreateSaga { SagaId = Guid.CreateVersion7() });
        var handler = new GetCreateStatusQueryHandler(_sagaStates.Object);

        await handler.HandleAsync(
            new GetCreateStatusQuery { SagaId = Guid.CreateVersion7() },
            expectedToken
        );

        Assert.Equal(expectedToken, observedToken);
    }

    [Fact]
    public async Task HandleAsync_AsksTheReaderForTheCreateSagaType()
    {
        _sagaStates
            .Setup(reader => reader.ReadAsync<CreateSaga>(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateSaga { SagaId = Guid.CreateVersion7() });
        var handler = new GetCreateStatusQueryHandler(_sagaStates.Object);

        await handler.HandleAsync(
            new GetCreateStatusQuery { SagaId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken
        );

        _sagaStates.Verify(
            reader => reader.ReadAsync<CreateSaga>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}

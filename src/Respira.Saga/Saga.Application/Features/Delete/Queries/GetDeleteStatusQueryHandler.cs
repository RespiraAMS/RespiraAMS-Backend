using Respira.Saga.Application.Constracts.Sagas;
using Respira.Saga.Application.Features.Delete.Commands;
using Respira.Saga.Application.Features.Delete.Queries;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Saga.Application.Features.Delete.Queries
{
    /// <summary>
    /// Reads the persisted saga state. Wolverine deletes a saga as soon as it completes, so
    /// only in-flight runs can be observed here.
    /// </summary>
    public class GetDeleteStatusQueryHandler(ISagaStateReader sagaStates)
        : IQueryHandler<GetDeleteStatusQuery, Result<DeleteStatusResult>>
    {
        public async Task<Result<DeleteStatusResult>> HandleAsync(
            GetDeleteStatusQuery query,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(query);

            var saga = await sagaStates.ReadAsync<DeleteSaga>(
                query.SagaId,
                cancellationToken
            );

            if (saga is null)
            {
                return Result<DeleteStatusResult>.Failure(
                    new Error(
                        ApplicationStatus.ResourceNotFound,
                        "No profile deletion with this id is currently in progress; it has either finished or never started"
                    )
                );
            }

            return Result<DeleteStatusResult>.Success(
                ApplicationStatus.Success,
                new DeleteStatusResult
                {
                    SagaId = saga.SagaId,
                    Step = saga.Step,
                    AccountId = saga.AccountId,
                    DoctorId = saga.DoctorId,
                    MediaId = saga.MediaId,
                    FailureReason = saga.FailureReason,
                }
            );
        }
    }
}

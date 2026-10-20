using Respira.Saga.Application.Constracts.Sagas;
using Respira.Saga.Application.Features.Update.Commands;
using Respira.Saga.Application.Features.Update.Queries;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Saga.Application.Features.Update.Queries
{
    /// <summary>
    /// Reads the persisted saga state. Wolverine deletes a saga as soon as it completes, so
    /// only in-flight runs can be observed here.
    /// </summary>
    public class GetUpdateStatusQueryHandler(ISagaStateReader sagaStates)
        : IQueryHandler<GetUpdateStatusQuery, Result<UpdateStatusResult>>
    {
        public async Task<Result<UpdateStatusResult>> HandleAsync(
            GetUpdateStatusQuery query,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(query);

            var saga = await sagaStates.ReadAsync<UpdateSaga>(
                query.SagaId,
                cancellationToken
            );

            if (saga is null)
            {
                return Result<UpdateStatusResult>.Failure(
                    new ApplicationError(
                        ApplicationStatus.ResourceNotFound,
                        "No profile update with this id is currently in progress; it has either finished or never started"
                    )
                );
            }

            return Result<UpdateStatusResult>.Success(
                ApplicationStatus.Success,
                new UpdateStatusResult
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

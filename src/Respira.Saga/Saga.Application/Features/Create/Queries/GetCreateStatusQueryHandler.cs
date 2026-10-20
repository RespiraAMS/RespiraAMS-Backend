using Respira.Saga.Application.Constracts.Sagas;
using Respira.Saga.Application.Features.Create.Commands;
using Respira.Saga.Application.Features.Create.Queries;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Saga.Application.Features.Create.Queries
{
    /// <summary>
    /// Reads the persisted saga state. Wolverine deletes a saga as soon as it completes, so
    /// only in-flight runs can be observed here.
    /// </summary>
    public class GetCreateStatusQueryHandler(ISagaStateReader sagaStates)
        : IQueryHandler<GetCreateStatusQuery, Result<CreateStatusResult>>
    {
        public async Task<Result<CreateStatusResult>> HandleAsync(
            GetCreateStatusQuery query,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(query);

            var saga = await sagaStates.ReadAsync<CreateSaga>(
                query.SagaId,
                cancellationToken
            );

            if (saga is null)
            {
                return Result<CreateStatusResult>.Failure(
                    new ApplicationError(
                        ApplicationStatus.ResourceNotFound,
                        "No profile creation with this id is currently in progress; it has either finished or never started"
                    )
                );
            }

            return Result<CreateStatusResult>.Success(
                ApplicationStatus.Success,
                new CreateStatusResult
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

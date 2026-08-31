using System.Text.Json;
using Application.Abstracts.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Constracts.CQRS;
using Respira.ServiceDefaults.Dtos;

namespace Respira.SagaAudit.Application.Features.GetSaga.Queries
{
    /// <summary>
    /// Loads the process tracker for a saga and maps it to <see cref="GetSagaResult"/>.
    /// </summary>
    public class GetSagaQueryHandler(
        ISagaAuditDbContext dbContext,
        ILogger<GetSagaQueryHandler> logger
    ) : IQueryHandler<GetSagaQuery, ApiResponse<GetSagaResult>>
    {
        public async Task<ApiResponse<GetSagaResult>> HandleAsync(
            GetSagaQuery query,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var tracker = await dbContext
                    .ProcessTrackers.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.SagaId == query.SagaId, cancellationToken);

                if (tracker is null)
                {
                    return ApiResponse<GetSagaResult>.Fail(
                        "Saga not found",
                        StatusCodes.Status404NotFound
                    );
                }

                return ApiResponse<GetSagaResult>.Ok(
                    new GetSagaResult
                    {
                        SagaId = tracker.SagaId,
                        SagaType = tracker.SagaType,
                        Status = tracker.Status.ToString(),
                        CurrentStep = tracker.CurrentStep,
                        Steps = JsonSerializer.Deserialize<object[]>(tracker.StepsJson ?? "[]"),
                        FailureReason = tracker.FailureReason,
                        ManagerId = tracker.ManagerId,
                        TargetDoctorId = tracker.TargetDoctorId,
                        CreatedAt = tracker.CreatedAt,
                        UpdatedAt = tracker.UpdatedAt,
                    }
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving saga {SagaId}", query.SagaId);
                return ApiResponse<GetSagaResult>.Fail("Error retrieving saga");
            }
        }
    }
}

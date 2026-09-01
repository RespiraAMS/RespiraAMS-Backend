using Application.Abstracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Dtos;

namespace Respira.SagaAudit.Application.Features.ListSagas.Queries
{
    /// <summary>
    /// Lists recent sagas (newest first) with an optional status filter.
    /// </summary>
    public class ListSagasQueryHandler(
        ISagaAuditDbContext dbContext,
        ILogger<ListSagasQueryHandler> logger
    ) : IQueryHandler<ListSagasQuery, ApiResponse<List<ListSagasResult>>>
    {
        public async Task<ApiResponse<List<ListSagasResult>>> HandleAsync(
            ListSagasQuery query,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                var sagas = await dbContext
                    .ProcessTrackers.AsNoTracking()
                    .AsQueryable()
                    .Where(x => !query.Status.HasValue || x.Status == query.Status.Value)
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(query.Limit)
                    .Select(x => new ListSagasResult
                    {
                        SagaId = x.SagaId,
                        SagaType = x.SagaType,
                        Status = x.Status.ToString(),
                        CurrentStep = x.CurrentStep,
                        FailureReason = x.FailureReason,
                        ManagerId = x.ManagerId,
                        TargetDoctorId = x.TargetDoctorId,
                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt,
                    })
                    .ToListAsync(cancellationToken);

                return ApiResponse<List<ListSagasResult>>.Ok(sagas);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error listing sagas");
                return ApiResponse<List<ListSagasResult>>.Fail("Error listing sagas");
            }
        }
    }
}

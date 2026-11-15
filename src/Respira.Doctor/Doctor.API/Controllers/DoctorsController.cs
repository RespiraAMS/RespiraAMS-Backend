using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Doctor.API.Dtos;
using Respira.Doctor.Application.Features.Doctors.Queries.GetByAccountId;
using Respira.Doctor.Application.Features.Doctors.Queries.GetById;
using Respira.Doctor.Application.Features.Doctors.Queries.GetById.Result;
using Respira.Doctor.Application.Features.Doctors.Queries.GetPagedDoctor;
using Respira.Doctor.Application.Features.Doctors.Queries.SearchDoctor;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Doctor.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/doctors")]
    [Tags("Doctors")]
    public class DoctorsController(IMessageBus bus) : ControllerBase
    {
        /// <summary>
        /// Retrieves a paged list of doctor and head of department doctor profiles.
        /// Restricted to Admin only.
        /// </summary>
        /// <param name="req">Pagination and filter parameters.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>A paged list of profiles enriched with account and media data.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(Result<Pagination<PagedDoctorItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPaged(
            [FromQuery] GetPagedDoctorRequestDto req,
            CancellationToken cancellationToken
        )
        {
            if (!TryAuthorizeAdmin(out var failureResult))
            {
                return failureResult!;
            }

            var result = await bus.InvokeAsync<Result<Pagination<PagedDoctorItem>>>(
                req.ToQuery(),
                cancellationToken
            );

            return result.ToApiResponse();
        }

        /// <summary>
        /// Retrieves a paged list of subordinate doctor profiles managed by the calling head of department doctor.
        /// </summary>
        /// <param name="req">Pagination and filter parameters.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>A paged list of subordinate doctor profiles enriched with account and media data.</returns>
        [HttpGet("subordinates")]
        [ProducesResponseType(typeof(Result<Pagination<PagedDoctorItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSubordinates(
            [FromQuery] GetSubordinateDoctorsRequestDto req,
            CancellationToken cancellationToken
        )
        {
            if (!TryResolveHeadDoctorCaller(out var headDoctorAccountId, out var failureResult))
            {
                return failureResult!;
            }

            var result = await bus.InvokeAsync<Result<Pagination<PagedDoctorItem>>>(
                req.ToQuery(headDoctorAccountId),
                cancellationToken
            );

            return result.ToApiResponse();
        }

        /// <summary>
        /// Searches doctor and head of department doctor profiles by first name, last name
        /// or citizen id.
        /// </summary>
        /// <param name="req">Pagination and search parameters.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>A paged list of matching profiles enriched with account and media data.</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(Result<Pagination<SearchDoctorItem>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Search(
            [FromQuery] SearchDoctorRequestDto req,
            CancellationToken cancellationToken
        )
        {
            var result = await bus.InvokeAsync<Result<Pagination<SearchDoctorItem>>>(
                req.ToQuery(),
                cancellationToken
            );

            return result.ToApiResponse();
        }

        /// <summary>
        /// Retrieves the currently authenticated doctor or head of department doctor profile.
        /// </summary>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>The profile of the authenticated doctor, or unauthorized/not-found failure.</returns>
        [HttpGet("me")]
        [ProducesResponseType(typeof(Result<DoctorResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(Result<DoctorResult>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentDoctor(CancellationToken cancellationToken)
        {
            if (!TryResolveCallerAccountId(out var accountId, out var failureResult))
            {
                return failureResult!;
            }

            var result = await bus.InvokeAsync<Result<DoctorResult>>(
                new GetDoctorByAccountIdQuery { AccountId = accountId },
                cancellationToken
            );

            return result.ToApiResponse();
        }

        /// <summary>
        /// Retrieves a single doctor or head of department doctor profile by its
        /// identifier.
        /// </summary>
        /// <param name="doctorId">Identifier of the doctor profile.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>The matching profile, or a resource-not-found failure.</returns>
        [HttpGet("{doctorId:guid}")]
        [ProducesResponseType(typeof(Result<DoctorResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Result<DoctorResult>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(
            Guid doctorId,
            CancellationToken cancellationToken
        )
        {
            var result = await bus.InvokeAsync<Result<DoctorResult>>(
                new GetDoctorByIdQuery { DoctorId = doctorId },
                cancellationToken
            );

            return result.ToApiResponse();
        }

        private bool TryAuthorizeAdmin(out IActionResult? failureResult)
        {
            var role = User.FindFirstValue(ClaimTypes.Role)
                ?? Request.Headers["X-Role"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(role))
            {
                failureResult = StatusCode(
                    StatusCodes.Status401Unauthorized,
                    Result.Failure(new ApplicationError(ApplicationStatus.Unauthorized, "Missing authentication credentials"))
                );
                return false;
            }

            if (!role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                failureResult = StatusCode(
                    StatusCodes.Status403Forbidden,
                    Result.Failure(new ApplicationError(ApplicationStatus.Restricted, "Forbidden: Only Admin can access the full doctor and head department doctor list"))
                );
                return false;
            }

            failureResult = null;
            return true;
        }

        private bool TryResolveHeadDoctorCaller(out Guid accountId, out IActionResult? failureResult)
        {
            var role = User.FindFirstValue(ClaimTypes.Role)
                ?? Request.Headers["X-Role"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(role))
            {
                failureResult = StatusCode(
                    StatusCodes.Status401Unauthorized,
                    Result.Failure(new ApplicationError(ApplicationStatus.Unauthorized, "Missing authentication credentials"))
                );
                accountId = Guid.Empty;
                return false;
            }

            if (!role.Equals("HeadDepartmentDoctor", StringComparison.OrdinalIgnoreCase))
            {
                failureResult = StatusCode(
                    StatusCodes.Status403Forbidden,
                    Result.Failure(new ApplicationError(ApplicationStatus.Restricted, "Forbidden: Only HeadDepartmentDoctor can access subordinate doctors"))
                );
                accountId = Guid.Empty;
                return false;
            }

            return TryResolveCallerAccountId(out accountId, out failureResult);
        }

        private bool TryResolveCallerAccountId(out Guid accountId, out IActionResult? failureResult)
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? Request.Headers["X-ID"].FirstOrDefault();

            if (!Guid.TryParse(idStr, out accountId) || accountId == Guid.Empty)
            {
                failureResult = StatusCode(
                    StatusCodes.Status401Unauthorized,
                    Result.Failure(new ApplicationError(ApplicationStatus.Unauthorized, "Missing or invalid account identifier"))
                );
                accountId = Guid.Empty;
                return false;
            }

            failureResult = null;
            return true;
        }
    }
}

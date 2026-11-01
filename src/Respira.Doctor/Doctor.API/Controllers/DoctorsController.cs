using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Doctor.API.Dtos;
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
        /// Retrieves a paged list of doctor and head of department doctor profiles,
        /// optionally filtered by role, gender, academic title or hospital position.
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
            var result = await bus.InvokeAsync<Result<Pagination<PagedDoctorItem>>>(
                req.ToQuery(),
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
    }
}

using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Respira.Doctor.Application.Features.Doctors.Queries.GetById;
using Respira.Doctor.Application.Features.Doctors.Queries.GetById.Result;
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
        /// Retrieves a single doctor (or manager) profile by its identifier.
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

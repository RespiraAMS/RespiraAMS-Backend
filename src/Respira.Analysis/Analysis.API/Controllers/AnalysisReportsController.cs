using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Respira.Analysis.API.Dtos;
using Respira.Analysis.Application.Contracts;
using Respira.Analysis.Application.Contracts.Export;
using Respira.Analysis.Application.Features.Commands.Create;
using Respira.Analysis.Application.Features.Commands.DeleteAnalysisReport;
using Respira.Analysis.Application.Features.Queries.ExportAnalysisReportExcel;
using Respira.Analysis.Application.Features.Queries.GetAnalysisReport;
using Respira.Analysis.Application.Features.Queries.GetPagedAnalysisReport;
using Respira.ServiceDefaults.Contracts.Pagination;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Analysis.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/analysis/reports")]
[Authorize(Roles = "Admin,HeadDepartmentDoctor,Doctor")]
[Tags("Analysis")]
public sealed class AnalysisReportsController(IMessageBus bus) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Result<AnalysisReportResult>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<AnalysisReportResult>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAnalysisReportCommand request,
        CancellationToken cancellationToken
    )
    {
        if (!TryResolveDoctorScope(request.DoctorId, out var doctorId))
            return Forbid();

        var result = await bus.InvokeAsync<Result<AnalysisReportResult>>(
            request with { DoctorId = doctorId },
            cancellationToken
        );
        return result.ToApiResponse();
    }

    [HttpPost("{doctorId:guid}")]
    [ProducesResponseType(typeof(Result<AnalysisReportResult>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result<AnalysisReportResult>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateByDoctor(
        Guid doctorId,
        [FromBody] CreateAnalysisReportCommand request,
        CancellationToken cancellationToken
    )
    {
        if (doctorId == Guid.Empty)
            return BadRequest();

        if (!TryResolveDoctorScope(doctorId, out var scopedDoctorId))
            return Forbid();

        var command = request with { DoctorId = scopedDoctorId };
        var result = await bus.InvokeAsync<Result<AnalysisReportResult>>(command, cancellationToken);
        return result.ToApiResponse();
    }

    [HttpGet]
    [ProducesResponseType<Result<Pagination<AnalysisReportListItem>>>(
        StatusCodes.Status200OK
    )]
    [ProducesResponseType<Result>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Result>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] GetPagedAnalysisReportRequestDto request,
        CancellationToken cancellationToken
    )
    {
        if (!TryResolveDoctorScope(request.DoctorId, out var doctorId))
            return Forbid();

        var result = await bus.InvokeAsync<Result<Pagination<AnalysisReportListItem>>>(
            request.ToQuery(doctorId),
            cancellationToken
        );

        return result.ToApiResponse();
    }

    [HttpGet("{analysisId:guid}")]
    [ProducesResponseType(typeof(Result<AnalysisReportResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<AnalysisReportResult>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        Guid analysisId,
        CancellationToken cancellationToken
    )
    {
        if (!TryResolveDoctorScope(null, out var requiredDoctorId))
            return Forbid();

        var result = await bus.InvokeAsync<Result<AnalysisReportResult>>(
            new GetAnalysisReportQuery
            {
                AnalysisId = analysisId,
                RequiredDoctorId = requiredDoctorId,
            },
            cancellationToken
        );

        return result.ToApiResponse();
    }

    [HttpDelete("{analysisId:guid}")]
    [ProducesResponseType(typeof(Result<DeleteAnalysisReportResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid analysisId,
        CancellationToken cancellationToken
    )
    {
        var result = await bus.InvokeAsync<Result<DeleteAnalysisReportResult>>(
            new DeleteAnalysisReportCommand
            {
                AnalysisId = analysisId,
                ActorAccountId = ResolveCallerId(),
                ActorRole = ResolveCallerRole(),
            },
            cancellationToken
        );

        return result.ToApiResponse();
    }

    [HttpGet("{analysisId:guid}/excel")]
    [Produces(
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/json"
    )]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(Result<AnalysisReportExcelFile>),
        StatusCodes.Status400BadRequest
    )]
    [ProducesResponseType(
        typeof(Result<AnalysisReportExcelFile>),
        StatusCodes.Status404NotFound
    )]
    public async Task<IActionResult> ExportExcel(
        Guid analysisId,
        CancellationToken cancellationToken
    )
    {
        if (!TryResolveDoctorScope(null, out var requiredDoctorId))
            return Forbid();

        var result = await bus.InvokeAsync<Result<AnalysisReportExcelFile>>(
            new ExportAnalysisReportExcelQuery
            {
                AnalysisId = analysisId,
                RequiredDoctorId = requiredDoctorId,
            },
            cancellationToken
        );

        if (result.IsFailure() || result.Data is null)
        {
            return result.ToApiResponse();
        }

        return File(result.Data.Content, result.Data.ContentType, result.Data.FileName);
    }

    /// <summary>
    /// Ensures doctors can only query/create/read reports scoped to their own account ID,
    /// while Admin and HeadDepartmentDoctor retain unrestricted access.
    /// </summary>
    private bool TryResolveDoctorScope(Guid? requestedDoctorId, out Guid? effectiveDoctorId)
    {
        effectiveDoctorId = requestedDoctorId;
        if (!User.IsInRole("Doctor"))
            return true;

        var callerId = ResolveCallerId();
        if (callerId == Guid.Empty)
            return false;

        if (requestedDoctorId.HasValue && requestedDoctorId.Value != callerId)
            return false;

        effectiveDoctorId = callerId;
        return true;
    }

    private Guid ResolveCallerId() =>
        Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"),
            out var callerId
        )
            ? callerId
            : Guid.Empty;

    private string ResolveCallerRole() =>
        User.IsInRole("Admin")
            ? "Admin"
            : User.IsInRole("Doctor")
                ? "Doctor"
                : User.IsInRole("HeadDepartmentDoctor")
                    ? "HeadDepartmentDoctor"
                    : User.IsInRole("MedicalRegulationReviewer")
                        ? "MedicalRegulationReviewer"
                        : string.Empty;
}

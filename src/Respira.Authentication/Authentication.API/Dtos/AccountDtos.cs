using Respira.Authentication.Application.Features.Authentication.Queries.GetList;
using Respira.Authentication.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Authentication.API.Dtos;

/// <summary>
/// Query string parameters for the paged account list endpoint.
/// </summary>
public record GetListAccountRequestDto
{
    /// <summary>Pagination parameter: page index (1-based).</summary>
    public int Page { get; set; } = 1;

    /// <summary>Pagination parameter: page size.</summary>
    public int Size { get; set; } = 10;

    /// <summary>
    /// Optional role filter (e.g. <see cref="RoleType.HeadDepartmentDoctor"/> or
    /// <see cref="RoleType.MedicalRegulationReviewer"/>).
    /// </summary>
    public RoleType? Role { get; set; }

    /// <summary>
    /// Optional account status filter (<see cref="StatusType.Active"/> or
    /// <see cref="StatusType.Inactive"/>).
    /// </summary>
    public StatusType? Status { get; set; }

    /// <summary>
    /// Optional filter on whether the account's email address is confirmed.
    /// </summary>
    public bool? IsEmailConfirmed { get; set; }

    public GetListAccountQuery ToQuery(RoleType? callerRole = null)
    {
        return new GetListAccountQuery
        {
            PaginationParam = new PaginationParam { Page = Page, Size = Size },
            Filter = new AccountFilter
            {
                Role = callerRole == RoleType.HeadDepartmentDoctor ? RoleType.Doctor : Role,
                Status = Status,
                IsEmailConfirmed = IsEmailConfirmed,
            },
            CallerRole = callerRole,
        };
    }
}

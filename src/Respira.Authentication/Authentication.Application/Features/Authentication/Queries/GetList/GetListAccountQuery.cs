using Respira.Authentication.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Authentication.Application.Features.Authentication.Queries.GetList
{
    /// <summary>
    /// Filter criteria applied to the paged account list.
    /// </summary>
    public record AccountFilter
    {
        /// <summary>
        /// Role the accounts have to have (for example <see cref="RoleType.HeadDepartmentDoctor"/>
        /// or <see cref="RoleType.MedicalRegulationReviewer"/>). When not set, every allowed role is listed.
        /// </summary>
        public RoleType? Role { get; init; }

        /// <summary>
        /// Account status filter (<see cref="StatusType.Active"/> or <see cref="StatusType.Inactive"/>).
        /// </summary>
        public StatusType? Status { get; init; }

        /// <summary>
        /// Whether the account email address has been confirmed.
        /// </summary>
        public bool? IsEmailConfirmed { get; init; }
    }

    /// <summary>
    /// Lists the accounts of the system page by page, optionally filtered by role,
    /// status and email confirmation state.
    /// </summary>
    public record GetListAccountQuery : IQuery
    {
        /// <summary>Pagination parameter.</summary>
        public PaginationParam PaginationParam { get; init; } = new();

        /// <summary>Account filter criteria.</summary>
        public AccountFilter? Filter { get; init; } = new();

        /// <summary>
        /// Role of the authenticated caller requesting the list.
        /// <see cref="RoleType.Admin"/> may view any role; <see cref="RoleType.HeadDepartmentDoctor"/>
        /// is restricted to <see cref="RoleType.Doctor"/> accounts.
        /// </summary>
        public RoleType? CallerRole { get; init; }
    }
}

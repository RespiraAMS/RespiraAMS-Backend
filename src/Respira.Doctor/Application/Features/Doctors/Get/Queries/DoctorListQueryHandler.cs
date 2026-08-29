using Application.Abstracts.Data;
using Application.Contracts.Messages;
using Application.Features.Doctors.Get.Results;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Respira.ServiceDefaults.Constracts.CQRS;
using Respira.ServiceDefaults.Dtos;
using Respira.ServiceDefaults.Exceptions;
using Wolverine;

namespace Application.Features.Doctors.Get.Queries;

/// <summary>
/// Handles <see cref="DoctorListQuery"/>: filters and paginates the local doctor
/// profiles, then fetches the matching auth details from the Auth service in a
/// single batched <c>InvokeAsync</c> request/reply call (avoids N+1). Returns the
/// shared <see cref="Pagination{T}"/> envelope. Each doctor's <c>Subordinates</c>
/// (doctors they manage) are expanded to full rows so the data can drive later CRUD.
/// </summary>
/// <param name="dbContext">Doctor database context.</param>
/// <param name="bus">Message bus used for the batched auth lookup.</param>
public class DoctorListQueryHandler(
    IDoctorDbContext dbContext,
    IMessageBus bus
) : IQueryHandler<DoctorListQuery, Pagination<DoctorListItemResult>>
{
    /// <summary>
    /// Applies local filtering/paging, enriches with auth data and returns the page.
    /// </summary>
    /// <param name="query">The list query (pagination + optional filter).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paged, auth-enriched doctor list.</returns>
    public async Task<Pagination<DoctorListItemResult>> HandleAsync(
        DoctorListQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var page = query.Param.Page < 1 ? 1 : query.Param.Page;
        var size = query.Param.Size < 1 ? 10 : query.Param.Size;

        IQueryable<Doctor> baseQuery = dbContext.Doctors.AsNoTracking().Include(d => d.Subordinates);

        var search = query.Filter?.Search?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(search))
        {
            baseQuery = baseQuery.Where(d =>
                d.FirstName.ToLower().Contains(search) || d.LastName.ToLower().Contains(search)
            );
        }

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var doctors = await baseQuery
            .OrderBy(d => d.LastName)
            .ThenBy(d => d.FirstName)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        // Collect every id we need auth data for: each page row's own id + the ids of
        // its subordinates (single level). One batched call avoids N+1.
        var allIds = doctors
            .SelectMany(d =>
                new[] { d.Id }.Concat(d.Subordinates?.Select(s => s.Id) ?? Array.Empty<Guid>())
            )
            .Distinct()
            .ToList();

        var authById = new Dictionary<Guid, GetAuthDoctorListResult>();
        if (allIds.Count > 0)
        {
            var authReply = await bus.InvokeAsync<ApiResponse<IEnumerable<GetAuthDoctorListResult>>>(
                new GetListInfoDoctorQuery { Ids = allIds },
                cancellationToken
            );
            if (!authReply.Success)
            {
                throw new ServerException();
            }

            authById = authReply.Data!.ToDictionary(a => a.Id);
        }

        DoctorListItemResult Project(Doctor d)
        {
            authById.TryGetValue(d.Id, out var auth);
            return new DoctorListItemResult
            {
                Id = d.Id,
                Email = auth?.Email ?? string.Empty,
                Phone = auth?.Phone ?? string.Empty,
                Role = auth?.Role ?? string.Empty,
                IsEmailConfirmed = auth?.IsEmailConfirmed ?? false,
                Status = auth?.Status ?? string.Empty,
                FirstName = d.FirstName,
                LastName = d.LastName,
                Degrees = [.. d.Degrees.Select(x => x.ToString())],
                AcademicTitle = d.AcademicTitle.ToString(),
                Position = d.Position,
                Patients = d.Patients?.ToList(),
                Gender = d.Gender.ToString(),
                CitizenIdentificationNumber = d.CitizenIdentificationNumber,
                DateOfBirth = d.DateOfBirth,
                Address = d.Address,
                MediaId = d.MediaId,
                // Subordinates are loaded one level deep only (no ThenInclude), so the
                // nested items' own Subordinates stay null — no recursion.
                Subordinates = d.Subordinates?.Select(Project).ToList(),
            };
        }

        var items = doctors.Select(Project).ToList();

        var pageCount = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)size);
        var metadata = new PaginationMetadata
        {
            CurrentPage = page,
            PageSize = size,
            TotalItemCount = totalCount,
            PageCount = pageCount,
            HasNextPage = page < pageCount,
            HasPreviousPage = page > 1,
        };

        return new Pagination<DoctorListItemResult>(metadata, items);
    }
}

using Application.Features.Doctors.Get.Queries;
using Respira.ServiceDefaults.Dtos;

namespace Respira.Doctor.API.Dtos;

/// <summary>
/// HTTP binding model for the doctor list endpoint. Maps to
/// <see cref="DoctorListQuery"/> using the shared <see cref="PaginationParam"/>.
/// </summary>
public class GetDoctorsRequestDto
{
    /// <summary>Page index (1-based).</summary>
    public int Page { get; set; } = 1;

    /// <summary>Page size.</summary>
    public int Size { get; set; } = 10;

    /// <summary>Optional case-insensitive search across first/last name.</summary>
    public string? Search { get; set; }

    /// <summary>Maps this request to the <see cref="DoctorListQuery"/> message.</summary>
    public DoctorListQuery ToQuery()
    {
        return new DoctorListQuery
        {
            Param = new PaginationParam { Page = Page, Size = Size },
            Filter = new DoctorListFilter { Search = Search },
        };
    }
}

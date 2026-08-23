using System.Net.Http.Json;
using Respira.ServiceDefaults.Dtos;

namespace Respira.Doctor.API.Clients;

/// <summary>
/// HTTP client for calling the Auth service to get doctor account info.
/// </summary>
public class AuthClient(HttpClient http)
{
    public async Task<AuthDoctorInfo?> GetDoctorAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await http.GetAsync($"/api/v1/auth/doctors/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthDoctorInfo>>(cancellationToken);
        return result?.Data;
    }
}

public record AuthDoctorInfo
{
    public Guid Id { get; init; }
    public required string Email { get; init; }
    public required string Phone { get; init; }
    public required string Role { get; init; }
    public bool IsEmailConfirmed { get; init; }
    public required string Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

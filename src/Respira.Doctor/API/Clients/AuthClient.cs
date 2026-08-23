using System.Net.Http.Json;
using Respira.ServiceDefaults.Dtos;

namespace Respira.Doctor.API.Clients;

/// <summary>
/// HTTP client for calling the Auth service to get doctor account info.
/// </summary>
public class AuthClient(HttpClient http)
{
    /// <summary>
    /// Fetches a doctor's account information from the Auth service by ID.
    /// Returns <c>null</c> when the Auth service responds with a non-success status.
    /// </summary>
    /// <param name="id">Doctor (account) identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
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

/// <summary>
/// Doctor account information returned by the Auth service.
/// </summary>
public record AuthDoctorInfo
{
    /// <summary>Doctor (account) identifier</summary>
    public Guid Id { get; init; }

    /// <summary>Login email address</summary>
    public required string Email { get; init; }

    /// <summary>Contact phone number</summary>
    public required string Phone { get; init; }

    /// <summary>Account role (Doctor, Manager, Admin)</summary>
    public required string Role { get; init; }

    /// <summary>Whether the email address has been confirmed</summary>
    public bool IsEmailConfirmed { get; init; }

    /// <summary>Account status</summary>
    public required string Status { get; init; }

    /// <summary>Account creation timestamp</summary>
    public DateTimeOffset CreatedAt { get; init; }
}

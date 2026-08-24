using System.Net.Http.Json;

namespace Application.Clients;

/// <summary>
/// Contract for retrieving authenticated doctor information from the Auth service.
/// </summary>
public interface IAuthClient
{
    /// <summary>
    /// Retrieves authenticated doctor information by ID.
    /// </summary>
    Task<AuthDoctorDto> GetDoctorAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO representing authenticated doctor information returned by the Auth service.
/// </summary>
public record AuthDoctorDto
{
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public required string Role { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public required string Status { get; set; }
}

/// <summary>
/// Typed HTTP client for the Auth service, enabling Doctor API to fetch
/// authenticated doctor details via REST rather than Wolverine messaging.
/// Service discovery for <c>auth-service</c> is configured by AddServiceDefaults.
/// </summary>
public class AuthClient(HttpClient http) : IAuthClient
{
    /// <summary>
    /// Retrieves authenticated doctor information by ID from the Auth service.
    /// </summary>
    /// <param name="id">The doctor identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task<AuthDoctorDto> GetDoctorAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        using var response = await http.GetAsync($"api/1.0/auth/doctors/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AuthDoctorDto>(cancellationToken)
            ?? throw new InvalidOperationException("Auth service returned an empty response");
    }
}

using System.Net.Http.Json;

namespace Application.Clients;

/// <summary>
/// Contract for retrieving a media asset URL from the Media service.
/// </summary>
public interface IMediaClient
{
    /// <summary>
    /// Retrieves the URL of a media asset by ID.
    /// </summary>
    Task<string> GetUrlAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Typed HTTP client for the Media service, enabling other services to fetch
/// media asset URLs via REST rather than Wolverine messaging.
/// Service discovery for <c>media-service</c> is configured by AddServiceDefaults.
/// </summary>
public class MediaClient(HttpClient http) : IMediaClient
{
    /// <summary>
    /// Retrieves the URL of a media asset by ID from the Media service.
    /// </summary>
    /// <param name="id">The media identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task<string> GetUrlAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync($"api/v1/media/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken)
            ?? string.Empty;
    }
}

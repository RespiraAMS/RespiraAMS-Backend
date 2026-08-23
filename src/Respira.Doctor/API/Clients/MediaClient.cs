using System.Net.Http.Json;
using Respira.ServiceDefaults.Dtos;

namespace Respira.Doctor.API.Clients;

/// <summary>
/// HTTP client for calling the Media service to get media asset info.
/// </summary>
public class MediaClient(HttpClient http)
{
    /// <summary>
    /// Fetches media asset metadata from the Media service by ID.
    /// Returns <c>null</c> when the Media service responds with a non-success status.
    /// </summary>
    /// <param name="id">Media asset identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<MediaAssetInfo?> GetMediaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await http.GetAsync($"/api/v1/media/{id}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<MediaAssetInfo>>(cancellationToken);
        return result?.Data;
    }
}

/// <summary>
/// Media asset metadata returned by the Media service.
/// </summary>
public record MediaAssetInfo
{
    /// <summary>Media asset identifier</summary>
    public Guid Id { get; init; }

    /// <summary>Original file name</summary>
    public required string FileName { get; init; }

    /// <summary>Public URL to access the asset (if available)</summary>
    public string? Url { get; init; }

    /// <summary>MIME content type (e.g. image/png)</summary>
    public string? ContentType { get; init; }

    /// <summary>File size in bytes (if known)</summary>
    public long? Size { get; init; }
}

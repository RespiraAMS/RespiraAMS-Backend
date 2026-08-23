using System.Net.Http.Json;
using Respira.ServiceDefaults.Dtos;

namespace Respira.Doctor.API.Clients;

/// <summary>
/// HTTP client for calling the Media service to get media asset info.
/// </summary>
public class MediaClient(HttpClient http)
{
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

public record MediaAssetInfo
{
    public Guid Id { get; init; }
    public required string FileName { get; init; }
    public string? Url { get; init; }
    public string? ContentType { get; init; }
    public long? Size { get; init; }
}

using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Respira.SagaAudit.API.Clients;

/// <summary>Result returned by the Media service <c>POST /api/v1/media/upload</c> endpoint.</summary>
public class MediaUploadResult
{
    [JsonPropertyName("mediaId")]
    public Guid MediaId { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

/// <summary>
/// Typed HTTP client that streams an avatar <see cref="IFormFile"/> to the Media service's
/// upload endpoint and returns the created media id. Service discovery for <c>media-service</c>
/// is configured by <c>AddServiceDefaults</c>.
/// </summary>
public class MediaUploadClient(HttpClient http)
{
    public async Task<Guid> UploadAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(file.OpenReadStream());
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            streamContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        }
        content.Add(streamContent, "file", file.FileName);

        using var response = await http.PostAsync("api/v1/media/upload", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MediaUploadResult>(
            cancellationToken: cancellationToken
        );

        if (result is null)
        {
            throw new InvalidOperationException("Invalid response from the Media upload endpoint");
        }

        return result.MediaId;
    }
}

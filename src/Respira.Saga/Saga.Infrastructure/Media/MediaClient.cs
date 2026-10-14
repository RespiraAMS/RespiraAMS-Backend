using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Respira.ServiceDefaults.Contracts.Results;
using Respira.Saga.Application.Constracts.Media;

namespace Respira.Saga.Infrastructure.Media
{
    /// <summary>
    /// Talks to the Media API to store and remove the avatars that arrive with the saga
    /// requests (<c>POST /api/v1/media-assets</c>, <c>DELETE /api/v1/media-assets/{id}</c>).
    /// </summary>
    /// <remarks>
    /// Response bodies are parsed with <see cref="JsonDocument"/> instead of
    /// deserializing <c>Result&lt;Guid&gt;</c> directly: <c>Result&lt;T&gt;</c> has a
    /// private constructor and its <c>Error</c> has several public ones, which
    /// System.Text.Json refuses to bind. Reading the few fields the saga needs keeps the
    /// client free of that coupling.
    /// </remarks>
    public class MediaClient(HttpClient httpClient, ILogger<MediaClient> logger)
        : IMediaClient
    {
        /// <inheritdoc />
        public async Task<Result<Guid>> UploadAvatarAsync(
            Stream content,
            string fileName,
            string folder,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(content);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return Result<Guid>.Failure(
                    new Error(ApplicationStatus.BadRequest, "A file name is required")
                );
            }

            using var form = new MultipartFormDataContent();
            using var file = new StreamContent(content);
            file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                "application/octet-stream"
            );
            form.Add(file, "file", fileName);

            var url = $"api/v1/media-assets?folder={Uri.EscapeDataString(folder)}";

            HttpResponseMessage response;

            try
            {
                response = await httpClient.PostAsync(url, form, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Media service is unreachable while uploading {FileName}",
                    fileName
                );

                return Result<Guid>.Failure(
                    new Error(
                        ApplicationStatus.ThirdPartyServiceFailure,
                        $"The media service could not be reached: {exception.Message}"
                    )
                );
            }

            using (response)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(body))
                {
                    return Result<Guid>.Failure(
                        new Error(
                            ApplicationStatus.ThirdPartyServiceFailure,
                            $"The media service responded with {(int)response.StatusCode} and an empty body"
                        )
                    );
                }

                // An upload without a returned identifier is a failure for the saga, so
                // the fallback stays empty.
                return ParseBody(
                    body,
                    Guid.Empty,
                    "The avatar could not be uploaded",
                    response.StatusCode,
                    fileName
                );
            }
        }

        /// <inheritdoc />
        public async Task<Result<Guid>> DeleteAssetAsync(
            Guid mediaId,
            CancellationToken cancellationToken = default
        )
        {
            if (mediaId == Guid.Empty)
            {
                return Result<Guid>.Failure(
                    new Error(ApplicationStatus.BadRequest, "A media id is required")
                );
            }

            HttpResponseMessage response;

            try
            {
                response = await httpClient.DeleteAsync(
                    $"api/v1/media-assets/{mediaId}",
                    cancellationToken
                );
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Media service is unreachable while deleting {MediaId}",
                    mediaId
                );

                return Result<Guid>.Failure(
                    new Error(
                        ApplicationStatus.ThirdPartyServiceFailure,
                        $"The media service could not be reached: {exception.Message}"
                    )
                );
            }

            using (response)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                // The delete endpoint answers 204 No Content on success; the identifier
                // that was asked for is all the caller needs back.
                if (string.IsNullOrWhiteSpace(body))
                {
                    return response.IsSuccessStatusCode
                        ? Result<Guid>.Success(ApplicationStatus.Deleted, mediaId)
                        : Result<Guid>.Failure(
                            new Error(
                                ApplicationStatus.ThirdPartyServiceFailure,
                                $"The media service responded with {(int)response.StatusCode} and an empty body"
                            )
                        );
                }

                return ParseBody(
                    body,
                    mediaId,
                    "The asset could not be deleted",
                    response.StatusCode,
                    mediaId.ToString()
                );
            }
        }

        /// <summary>
        /// Reads a <c>{"statusCode":..., "data":...}</c> / <c>{"statusCode":..., "error":{...}}</c>
        /// body. When a successful body carries no identifier, <paramref name="fallbackId"/>
        /// is used, which is what lets a delete answer with an empty payload.
        /// </summary>
        private Result<Guid> ParseBody(
            string body,
            Guid fallbackId,
            string failureMessage,
            HttpStatusCode statusCode,
            string context
        )
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                if (
                    root.TryGetProperty("statusCode", out var statusCodeElement)
                    && statusCodeElement.GetString() is string code
                    && ApplicationStatus.IsSuccess(code)
                )
                {
                    var data = fallbackId;

                    if (
                        root.TryGetProperty("data", out var dataElement)
                        && dataElement.ValueKind == JsonValueKind.String
                        && Guid.TryParse(dataElement.GetString(), out var parsed)
                    )
                    {
                        data = parsed;
                    }

                    return Result<Guid>.Success(code, data);
                }

                // Failure path: the body carries {"statusCode":..., "error":{...}}.
                var description = root.TryGetProperty("error", out var errorElement)
                        && errorElement.ValueKind == JsonValueKind.Object
                        && errorElement.TryGetProperty("description", out var descriptionElement)
                        && descriptionElement.ValueKind == JsonValueKind.String
                    ? descriptionElement.GetString()
                    : body;

                logger.LogError(
                    "Media service rejected {Context} with {StatusCode}: {Description}",
                    context,
                    (int)statusCode,
                    description
                );

                return Result<Guid>.Failure(
                    new Error(
                        ApplicationStatus.ThirdPartyServiceFailure,
                        $"{failureMessage}: {description}"
                    )
                );
            }
            catch (JsonException exception)
            {
                logger.LogError(
                    exception,
                    "Media service returned a body that is not JSON: {Body}",
                    body
                );

                return Result<Guid>.Failure(
                    new Error(
                        ApplicationStatus.ThirdPartyServiceFailure,
                        "The media service returned an unexpected response"
                    )
                );
            }
        }
    }
}

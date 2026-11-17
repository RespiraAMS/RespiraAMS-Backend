using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Respira.Saga.Infrastructure.Media;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Saga.Infrastructure.Test.Media;

/// <summary>
/// The media client is the only synchronous hop of the saga, so every failure mode of the
/// HTTP call has to be translated into a <see cref="Result{T}"/> the saga can compensate on.
/// These tests drive it through a stub handler and pin the request shape and the mapping of
/// the media API answers onto results.
/// </summary>
public sealed class MediaClientTest
{
    private static readonly Uri BaseAddress = new("https://media.test/");

    #region Upload

    [Fact]
    public async Task UploadAvatarAsync_NullContent_Throws()
    {
        var (client, _) = CreateClient(_ => JsonResponse("{\"statusCode\":\"SUCCESS\"}"));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.UploadAvatarAsync(null!, "avatar.png", "doctors", TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task UploadAvatarAsync_EmptyFileName_FailsWithBadRequest()
    {
        var (client, _) = CreateClient(_ => JsonResponse("{\"statusCode\":\"SUCCESS\"}"));

        using var content = new MemoryStream([1, 2, 3]);
        var result = await client.UploadAvatarAsync(
            content,
            string.Empty,
            "doctors",
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.BadRequest, result.Error!.StatusCode);
        Assert.Equal("A file name is required", result.Error.Description);
    }

    [Fact]
    public async Task UploadAvatarAsync_WhitespaceFileName_FailsWithBadRequest()
    {
        var (client, _) = CreateClient(_ => JsonResponse("{\"statusCode\":\"SUCCESS\"}"));

        using var content = new MemoryStream([1, 2, 3]);
        var result = await client.UploadAvatarAsync(
            content,
            "   ",
            "doctors",
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.BadRequest, result.Error!.StatusCode);
        Assert.Equal("A file name is required", result.Error.Description);
    }

    [Fact]
    public async Task UploadAvatarAsync_SuccessfulResponse_ReturnsTheReturnedIdentifier()
    {
        var mediaId = Guid.CreateVersion7();
        var (client, _) = CreateClient(_ =>
            JsonResponse($"{{\"statusCode\":\"SUCCESS\",\"data\":\"{mediaId}\"}}")
        );

        using var content = new MemoryStream([1, 2, 3]);
        var result = await client.UploadAvatarAsync(
            content,
            "avatar.png",
            "doctors",
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Success, result.StatusCode);
        Assert.Equal(mediaId, result.Data);
    }

    [Fact]
    public async Task UploadAvatarAsync_PostsToTheMediaAssetsEndpointWithAnEscapedFolder()
    {
        var folder = "doctors/my id/avatar";
        var (client, requests) = CreateClient(_ => JsonResponse("{\"statusCode\":\"SUCCESS\"}"));

        using var content = new MemoryStream([1, 2, 3]);
        await client.UploadAvatarAsync(content, "avatar.png", folder, TestContext.Current.CancellationToken);

        var request = Assert.Single(requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/v1/media-assets", request.RequestUri.AbsolutePath);
        Assert.Contains(Uri.EscapeDataString(folder), request.RequestUri.Query);
        Assert.StartsWith("https://media.test/", request.RequestUri.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task UploadAvatarAsync_SendsAMultipartBodyContainingTheFileName()
    {
        var (client, requests) = CreateClient(_ => JsonResponse("{\"statusCode\":\"SUCCESS\"}"));

        using var content = new MemoryStream([1, 2, 3]);
        await client.UploadAvatarAsync(
            content,
            "avatar.png",
            "doctors",
            TestContext.Current.CancellationToken
        );

        var request = Assert.Single(requests);
        Assert.Contains("multipart/form-data", request.ContentType, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("--", request.Body, StringComparison.Ordinal);
        Assert.Contains("avatar.png", request.Body, StringComparison.Ordinal);
        Assert.Contains("name=file", request.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UploadAvatarAsync_SendsTheActorAccountIdWhenOneIsGiven()
    {
        var actorAccountId = Guid.CreateVersion7();
        var (client, requests) = CreateClient(_ => JsonResponse("{\"statusCode\":\"SUCCESS\"}"));

        using var content = new MemoryStream([1, 2, 3]);
        await client.UploadAvatarAsync(
            content,
            "avatar.png",
            "doctors",
            TestContext.Current.CancellationToken,
            actorAccountId
        );

        var request = Assert.Single(requests);
        Assert.Contains("name=actorAccountId", request.Body, StringComparison.Ordinal);
        Assert.Contains(actorAccountId.ToString(), request.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UploadAvatarAsync_SendsTheActorRole()
    {
        var (client, requests) = CreateClient(_ => JsonResponse("{\"statusCode\":\"SUCCESS\"}"));

        using var content = new MemoryStream([1, 2, 3]);
        await client.UploadAvatarAsync(
            content,
            "avatar.png",
            "doctors",
            TestContext.Current.CancellationToken,
            actorRole: "HeadDepartmentDoctor"
        );

        var request = Assert.Single(requests);
        Assert.Contains("name=actorRole", request.Body, StringComparison.Ordinal);
        Assert.Contains("HeadDepartmentDoctor", request.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UploadAvatarAsync_OmitsTheActorAccountIdWhenThereIsNoActor()
    {
        var (client, requests) = CreateClient(_ => JsonResponse("{\"statusCode\":\"SUCCESS\"}"));

        using var content = new MemoryStream([1, 2, 3]);
        await client.UploadAvatarAsync(content, "avatar.png", "doctors", TestContext.Current.CancellationToken);

        var request = Assert.Single(requests);
        Assert.DoesNotContain("actorAccountId", request.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UploadAvatarAsync_EmptyBody_FailsAndMentionsTheStatusCode()
    {
        var (client, _) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(string.Empty)
        });

        using var content = new MemoryStream([1, 2, 3]);
        var result = await client.UploadAvatarAsync(
            content,
            "avatar.png",
            "doctors",
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ThirdPartyServiceFailure, result.Error!.StatusCode);
        Assert.Contains("200", result.Error.Description, StringComparison.Ordinal);
        Assert.Contains("empty body", result.Error.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadAvatarAsync_NonJsonBody_FailsWithAnUnexpectedResponse()
    {
        var (client, _) = CreateClient(_ => PlainTextResponse("<html>gateway error</html>"));

        using var content = new MemoryStream([1, 2, 3]);
        var result = await client.UploadAvatarAsync(
            content,
            "avatar.png",
            "doctors",
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ThirdPartyServiceFailure, result.Error!.StatusCode);
        Assert.Equal("The media service returned an unexpected response", result.Error.Description);
    }

    [Fact]
    public async Task UploadAvatarAsync_FailureBody_FailsWithTheErrorDescription()
    {
        var (client, _) = CreateClient(_ =>
            JsonResponse("{\"statusCode\":\"FAILURE-SERVER-ERROR\",\"error\":{\"description\":\"boom\"}}")
        );

        using var content = new MemoryStream([1, 2, 3]);
        var result = await client.UploadAvatarAsync(
            content,
            "avatar.png",
            "doctors",
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ThirdPartyServiceFailure, result.Error!.StatusCode);
        Assert.Contains("boom", result.Error.Description, StringComparison.Ordinal);
        Assert.Contains("The avatar could not be uploaded", result.Error.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UploadAvatarAsync_FailureBodyWithoutAnError_FallsBackToTheRawBody()
    {
        const string body = "{\"statusCode\":\"FAILURE-SERVER-ERROR\"}";
        var (client, _) = CreateClient(_ => JsonResponse(body));

        using var content = new MemoryStream([1, 2, 3]);
        var result = await client.UploadAvatarAsync(
            content,
            "avatar.png",
            "doctors",
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ThirdPartyServiceFailure, result.Error!.StatusCode);
        Assert.Contains(body, result.Error.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UploadAvatarAsync_SuccessBodyWithANonGuidData_FallsBackToAnEmptyIdentifier()
    {
        var (client, _) = CreateClient(_ =>
            JsonResponse("{\"statusCode\":\"SUCCESS\",\"data\":\"not-a-guid\"}")
        );

        using var content = new MemoryStream([1, 2, 3]);
        var result = await client.UploadAvatarAsync(
            content,
            "avatar.png",
            "doctors",
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(Guid.Empty, result.Data);
    }

    [Fact]
    public async Task UploadAvatarAsync_SuccessBodyWithoutData_FallsBackToAnEmptyIdentifier()
    {
        var (client, _) = CreateClient(_ => JsonResponse("{\"statusCode\":\"SUCCESS\"}"));

        using var content = new MemoryStream([1, 2, 3]);
        var result = await client.UploadAvatarAsync(
            content,
            "avatar.png",
            "doctors",
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(Guid.Empty, result.Data);
    }

    [Fact]
    public async Task UploadAvatarAsync_SagaSuccessStatusCode_IsASuccess()
    {
        var mediaId = Guid.CreateVersion7();
        var (client, _) = CreateClient(_ =>
            JsonResponse($"{{\"statusCode\":\"SAGA-SUCCESS\",\"data\":\"{mediaId}\"}}")
        );

        using var content = new MemoryStream([1, 2, 3]);
        var result = await client.UploadAvatarAsync(
            content,
            "avatar.png",
            "doctors",
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.SagaSuccess, result.StatusCode);
        Assert.Equal(mediaId, result.Data);
    }

    [Fact]
    public async Task UploadAvatarAsync_UnreachableMediaService_FailsWithTheUnreachableMessage()
    {
        var (client, _) = CreateClient(_ => throw new HttpRequestException("connection refused"));

        using var content = new MemoryStream([1, 2, 3]);
        var result = await client.UploadAvatarAsync(
            content,
            "avatar.png",
            "doctors",
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ThirdPartyServiceFailure, result.Error!.StatusCode);
        Assert.Equal(
            "The media service could not be reached: connection refused",
            result.Error.Description
        );
    }

    [Fact]
    public async Task UploadAvatarAsync_CancelledToken_PropagatesTheCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var (client, _) = CreateClient(_ => throw new OperationCanceledException(cancellation.Token));

        using var content = new MemoryStream([1, 2, 3]);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.UploadAvatarAsync(content, "avatar.png", "doctors", cancellation.Token)
        );
    }

    [Fact]
    public async Task UploadAvatarAsync_TimeoutWithoutCancellation_FailsAsAThirdPartyFailure()
    {
        var (client, _) = CreateClient(_ => throw new OperationCanceledException("timed out"));

        using var content = new MemoryStream([1, 2, 3]);
        var result = await client.UploadAvatarAsync(
            content,
            "avatar.png",
            "doctors",
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ThirdPartyServiceFailure, result.Error!.StatusCode);
        Assert.Equal(
            "The media service could not be reached: timed out",
            result.Error.Description
        );
    }

    [Fact]
    public async Task UploadAvatarAsync_NonSuccessStatusCode_IsMentionedInTheFailureMessage()
    {
        var (client, _) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent(string.Empty)
        });

        using var content = new MemoryStream([1, 2, 3]);
        var result = await client.UploadAvatarAsync(
            content,
            "avatar.png",
            "doctors",
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ThirdPartyServiceFailure, result.Error!.StatusCode);
        Assert.Contains("503", result.Error.Description, StringComparison.Ordinal);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task DeleteAssetAsync_EmptyMediaId_FailsWithBadRequest()
    {
        var (client, _) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        var result = await client.DeleteAssetAsync(
            Guid.Empty,
            TestContext.Current.CancellationToken
        );

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.BadRequest, result.Error!.StatusCode);
        Assert.Equal("A media id is required", result.Error.Description);
    }

    [Fact]
    public async Task DeleteAssetAsync_NoContentResponse_SucceedsWithTheRequestedMediaId()
    {
        var mediaId = Guid.CreateVersion7();
        var (client, _) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NoContent)
        {
            Content = new StringContent(string.Empty)
        });

        var result = await client.DeleteAssetAsync(mediaId, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);
        Assert.Equal(mediaId, result.Data);
    }

    [Fact]
    public async Task DeleteAssetAsync_SuccessBody_ReturnsTheIdentifierFromTheBody()
    {
        var mediaId = Guid.CreateVersion7();
        var otherId = Guid.CreateVersion7();
        var (client, _) = CreateClient(_ =>
            JsonResponse($"{{\"statusCode\":\"SUCCESS-DELETED\",\"data\":\"{otherId}\"}}")
        );

        var result = await client.DeleteAssetAsync(mediaId, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess());
        Assert.Equal(ApplicationStatus.Deleted, result.StatusCode);
        Assert.Equal(otherId, result.Data);
    }

    [Fact]
    public async Task DeleteAssetAsync_DeletesTheAssetWithTheActorInTheQueryString()
    {
        var mediaId = Guid.CreateVersion7();
        var actorAccountId = Guid.CreateVersion7();
        var (client, requests) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        await client.DeleteAssetAsync(
            mediaId,
            TestContext.Current.CancellationToken,
            actorAccountId,
            "Admin"
        );

        var request = Assert.Single(requests);
        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal($"/api/v1/media-assets/{mediaId}", request.RequestUri.AbsolutePath);
        Assert.Contains($"actorAccountId={actorAccountId}", request.RequestUri.Query, StringComparison.Ordinal);
        Assert.Contains("actorRole=Admin", request.RequestUri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteAssetAsync_EscapesTheActorRoleInTheQueryString()
    {
        var mediaId = Guid.CreateVersion7();
        var (client, requests) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        await client.DeleteAssetAsync(
            mediaId,
            TestContext.Current.CancellationToken,
            actorRole: "Head Department Doctor"
        );

        var request = Assert.Single(requests);
        Assert.Contains(
            $"actorRole={Uri.EscapeDataString("Head Department Doctor")}",
            request.RequestUri.Query,
            StringComparison.Ordinal
        );
        Assert.DoesNotContain("Head Department Doctor", request.RequestUri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteAssetAsync_EmptyBodyWithAFailureStatus_FailsAndMentionsTheStatusCode()
    {
        var mediaId = Guid.CreateVersion7();
        var (client, _) = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(string.Empty)
        });

        var result = await client.DeleteAssetAsync(mediaId, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ThirdPartyServiceFailure, result.Error!.StatusCode);
        Assert.Contains("500", result.Error.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteAssetAsync_FailureBody_FailsWithTheErrorDescription()
    {
        var mediaId = Guid.CreateVersion7();
        var (client, _) = CreateClient(_ =>
            JsonResponse("{\"statusCode\":\"FAILURE-SERVER-ERROR\",\"error\":{\"description\":\"boom\"}}")
        );

        var result = await client.DeleteAssetAsync(mediaId, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ThirdPartyServiceFailure, result.Error!.StatusCode);
        Assert.Contains("boom", result.Error.Description, StringComparison.Ordinal);
        Assert.Contains("The asset could not be deleted", result.Error.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteAssetAsync_NonJsonBody_Fails()
    {
        var mediaId = Guid.CreateVersion7();
        var (client, _) = CreateClient(_ => PlainTextResponse("not json at all"));

        var result = await client.DeleteAssetAsync(mediaId, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ThirdPartyServiceFailure, result.Error!.StatusCode);
        Assert.Equal("The media service returned an unexpected response", result.Error.Description);
    }

    [Fact]
    public async Task DeleteAssetAsync_UnreachableMediaService_FailsWithTheUnreachableMessage()
    {
        var mediaId = Guid.CreateVersion7();
        var (client, _) = CreateClient(_ => throw new HttpRequestException("connection refused"));

        var result = await client.DeleteAssetAsync(mediaId, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ThirdPartyServiceFailure, result.Error!.StatusCode);
        Assert.Equal(
            "The media service could not be reached: connection refused",
            result.Error.Description
        );
    }

    [Fact]
    public async Task DeleteAssetAsync_CancelledToken_PropagatesTheCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var mediaId = Guid.CreateVersion7();
        var (client, _) = CreateClient(_ => throw new OperationCanceledException(cancellation.Token));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.DeleteAssetAsync(mediaId, cancellation.Token)
        );
    }

    [Fact]
    public async Task DeleteAssetAsync_TimeoutWithoutCancellation_FailsAsAThirdPartyFailure()
    {
        var mediaId = Guid.CreateVersion7();
        var (client, _) = CreateClient(_ => throw new OperationCanceledException("timed out"));

        var result = await client.DeleteAssetAsync(mediaId, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure());
        Assert.Equal(ApplicationStatus.ThirdPartyServiceFailure, result.Error!.StatusCode);
        Assert.Equal(
            "The media service could not be reached: timed out",
            result.Error.Description
        );
    }

    [Fact]
    public async Task DeleteAssetAsync_SuccessBodyWithoutData_ReturnsTheRequestedMediaId()
    {
        var mediaId = Guid.CreateVersion7();
        var (client, _) = CreateClient(_ => JsonResponse("{\"statusCode\":\"SUCCESS\"}"));

        var result = await client.DeleteAssetAsync(mediaId, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess());
        Assert.Equal(mediaId, result.Data);
    }

    #endregion

    #region Helpers

    private static (MediaClient Client, List<RecordedRequest> Requests) CreateClient(
        Func<HttpRequestMessage, HttpResponseMessage> responder
    )
    {
        var requests = new List<RecordedRequest>();

        var httpClient = new HttpClient(
            new StubHandler(request =>
            {
                requests.Add(
                    new RecordedRequest(
                        request.Method,
                        request.RequestUri!,
                        request.Content?.Headers.ContentType?.ToString() ?? string.Empty,
                        ReadBody(request)
                    )
                );
                return responder(request);
            })
        )
        {
            BaseAddress = BaseAddress,
        };

        return (new MediaClient(httpClient, Mock.Of<ILogger<MediaClient>>()), requests);
    }

    private static HttpResponseMessage JsonResponse(string body) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

    private static HttpResponseMessage PlainTextResponse(string body) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "text/plain"),
        };

    /// <summary>
    /// The request content is disposed by the client once the call returns, so it has to be
    /// read while the handler still owns it. The read runs on the thread pool so it cannot
    /// deadlock on the test scheduler.
    /// </summary>
    private static string ReadBody(HttpRequestMessage request) =>
        request.Content is null
            ? string.Empty
            : Task.Run(() => request.Content!.ReadAsStringAsync()).GetAwaiter().GetResult();

    private sealed record RecordedRequest(
        HttpMethod Method,
        Uri RequestUri,
        string ContentType,
        string Body
    );

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => Task.FromResult(responder(request));
    }

    #endregion
}

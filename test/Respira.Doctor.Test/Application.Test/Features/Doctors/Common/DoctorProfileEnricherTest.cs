using Microsoft.Extensions.Logging;
using Moq;
using Respira.Doctor.Application.Features.Doctors.Common;
using Respira.Doctor.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Messages.Accounts;
using Respira.ServiceDefaults.Contracts.Messages.Media;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Doctor.Application.Test.Features.Doctors.Common;
/// <summary>
/// Unit tests for <see cref="DoctorProfileEnricher"/>: the enricher resolves the
/// account and the media asset of every profile over the message bus and must
/// never let a downstream failure break the paged result of the caller.
/// </summary>
public sealed class DoctorProfileEnricherTest
{
    private readonly Mock<IMessageBus> _bus = new();
    private readonly DoctorProfileEnricher _enricher;

    public DoctorProfileEnricherTest()
    {
        var logger = Mock.Of<ILogger<DoctorProfileEnricher>>();

        _enricher = new DoctorProfileEnricher(_bus.Object, logger);
    }

    #region Happy path

    [Fact]
    public async Task EnrichAsync_WithEmptyCollection_DoesNotInvokeBus()
    {
        await _enricher.EnrichAsync([], TestContext.Current.CancellationToken);

        _bus.Verify(
            b =>
                b.InvokeAsync<Result<List<AccountSummary>>>(
                    It.IsAny<GetAccountsByIdsQuery>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                ),
            Times.Never
        );
        _bus.Verify(
            b =>
                b.InvokeAsync<Result<List<MediaAssetSummary>>>(
                    It.IsAny<GetMediaAssetsByIdsQuery>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task EnrichAsync_WithResolvedAccount_FillsAccountFields()
    {
        var accountId = Guid.CreateVersion7();
        var profile = BuildProfile(accountId: accountId);
        SetupAccounts(
            Result<List<AccountSummary>>.Success(
                ApplicationStatus.Success,
                [BuildAccount(accountId)]
            )
        );

        await _enricher.EnrichAsync([profile], TestContext.Current.CancellationToken);

        Assert.Equal("doctor@hospital.vn", profile.Email);
        Assert.Equal("+84901234567", profile.Phone);
        Assert.Equal("Doctor", profile.AccountRole);
        Assert.Equal("Active", profile.AccountStatus);
    }

    [Fact]
    public async Task EnrichAsync_WithSharedAccountId_FillsEveryMatchingProfile()
    {
        var accountId = Guid.CreateVersion7();
        var first = BuildProfile(accountId: accountId);
        var second = BuildProfile(accountId: accountId);
        SetupAccounts(
            Result<List<AccountSummary>>.Success(
                ApplicationStatus.Success,
                [BuildAccount(accountId)]
            )
        );

        await _enricher.EnrichAsync([first, second], TestContext.Current.CancellationToken);

        Assert.Equal("doctor@hospital.vn", first.Email);
        Assert.Equal("+84901234567", first.Phone);
        Assert.Equal("Doctor", first.AccountRole);
        Assert.Equal("Active", first.AccountStatus);

        Assert.Equal("doctor@hospital.vn", second.Email);
        Assert.Equal("+84901234567", second.Phone);
        Assert.Equal("Doctor", second.AccountRole);
        Assert.Equal("Active", second.AccountStatus);
    }

    [Fact]
    public async Task EnrichAsync_WithSharedAccountId_RequestsTheAccountIdOnlyOnce()
    {
        var accountId = Guid.CreateVersion7();
        var first = BuildProfile(accountId: accountId);
        var second = BuildProfile(accountId: accountId);
        SetupAccounts(
            Result<List<AccountSummary>>.Success(
                ApplicationStatus.Success,
                [BuildAccount(accountId)]
            )
        );

        await _enricher.EnrichAsync([first, second], TestContext.Current.CancellationToken);

        var query = GetAccountQuery();

        Assert.Single(query.AccountIds);
        Assert.Equal(accountId, query.AccountIds[0]);
        Assert.Single(AccountQueryInvocations());
    }

    [Fact]
    public async Task EnrichAsync_WithDistinctAccountIds_RequestsEachIdOnce()
    {
        var firstAccountId = Guid.CreateVersion7();
        var secondAccountId = Guid.CreateVersion7();
        var first = BuildProfile(accountId: firstAccountId);
        var second = BuildProfile(accountId: secondAccountId);
        SetupAccounts(
            Result<List<AccountSummary>>.Success(
                ApplicationStatus.Success,
                [BuildAccount(firstAccountId), BuildAccount(secondAccountId)]
            )
        );

        await _enricher.EnrichAsync([first, second], TestContext.Current.CancellationToken);

        var query = GetAccountQuery();

        Assert.Equal(2, query.AccountIds.Length);
        Assert.Contains(firstAccountId, query.AccountIds);
        Assert.Contains(secondAccountId, query.AccountIds);
        Assert.Single(AccountQueryInvocations());
    }

    [Fact]
    public async Task EnrichAsync_WithResolvedMediaAsset_FillsAvatarUrl()
    {
        var mediaId = Guid.CreateVersion7();
        var profile = BuildProfile(mediaId: mediaId);
        SetupMediaAssets(
            Result<List<MediaAssetSummary>>.Success(
                ApplicationStatus.Success,
                [BuildMediaAsset(mediaId)]
            )
        );

        await _enricher.EnrichAsync([profile], TestContext.Current.CancellationToken);

        Assert.Equal("https://cdn.hospital.vn/avatar.png", profile.AvatarUrl);
    }

    [Fact]
    public async Task EnrichAsync_WithoutMediaId_LeavesAvatarUrlNullAndSkipsMediaQuery()
    {
        var profile = BuildProfile();

        await _enricher.EnrichAsync([profile], TestContext.Current.CancellationToken);

        Assert.Null(profile.AvatarUrl);
        _bus.Verify(
            b =>
                b.InvokeAsync<Result<List<MediaAssetSummary>>>(
                    It.IsAny<GetMediaAssetsByIdsQuery>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task EnrichAsync_WithSharedMediaId_FillsEveryMatchingProfile()
    {
        var mediaId = Guid.CreateVersion7();
        var first = BuildProfile(mediaId: mediaId);
        var second = BuildProfile(mediaId: mediaId);
        SetupMediaAssets(
            Result<List<MediaAssetSummary>>.Success(
                ApplicationStatus.Success,
                [BuildMediaAsset(mediaId)]
            )
        );

        await _enricher.EnrichAsync([first, second], TestContext.Current.CancellationToken);

        Assert.Equal("https://cdn.hospital.vn/avatar.png", first.AvatarUrl);
        Assert.Equal("https://cdn.hospital.vn/avatar.png", second.AvatarUrl);
    }

    [Fact]
    public async Task EnrichAsync_WithSameItem_FillsAccountAndAvatarData()
    {
        var accountId = Guid.CreateVersion7();
        var mediaId = Guid.CreateVersion7();
        var profile = BuildProfile(accountId: accountId, mediaId: mediaId);
        SetupAccounts(
            Result<List<AccountSummary>>.Success(
                ApplicationStatus.Success,
                [BuildAccount(accountId)]
            )
        );
        SetupMediaAssets(
            Result<List<MediaAssetSummary>>.Success(
                ApplicationStatus.Success,
                [BuildMediaAsset(mediaId)]
            )
        );

        await _enricher.EnrichAsync([profile], TestContext.Current.CancellationToken);

        Assert.Equal("doctor@hospital.vn", profile.Email);
        Assert.Equal("+84901234567", profile.Phone);
        Assert.Equal("Doctor", profile.AccountRole);
        Assert.Equal("Active", profile.AccountStatus);
        Assert.Equal("https://cdn.hospital.vn/avatar.png", profile.AvatarUrl);
    }

    #endregion

    #region Boundaries

    [Fact]
    public async Task EnrichAsync_WhenAccountIsNotInTheResponse_LeavesAccountFieldsNull()
    {
        var profile = BuildProfile(accountId: Guid.CreateVersion7());
        SetupAccounts(
            Result<List<AccountSummary>>.Success(
                ApplicationStatus.Success,
                [BuildAccount(Guid.CreateVersion7())]
            )
        );

        await _enricher.EnrichAsync([profile], TestContext.Current.CancellationToken);

        Assert.Null(profile.Email);
        Assert.Null(profile.Phone);
        Assert.Null(profile.AccountRole);
        Assert.Null(profile.AccountStatus);
    }

    [Fact]
    public async Task EnrichAsync_WhenAccountResponseHasNoData_LeavesAccountFieldsNull()
    {
        var profile = BuildProfile();
        SetupAccounts(
            Result<List<AccountSummary>>.Success(ApplicationStatus.Success, null!)
        );

        await _enricher.EnrichAsync([profile], TestContext.Current.CancellationToken);

        Assert.Null(profile.Email);
        Assert.Null(profile.Phone);
        Assert.Null(profile.AccountRole);
        Assert.Null(profile.AccountStatus);
    }

    [Fact]
    public async Task EnrichAsync_WhenMediaAssetIsNotInTheResponse_LeavesAvatarUrlNull()
    {
        var profile = BuildProfile(mediaId: Guid.CreateVersion7());
        SetupMediaAssets(
            Result<List<MediaAssetSummary>>.Success(
                ApplicationStatus.Success,
                [BuildMediaAsset(Guid.CreateVersion7())]
            )
        );

        await _enricher.EnrichAsync([profile], TestContext.Current.CancellationToken);

        Assert.Null(profile.AvatarUrl);
    }

    [Fact]
    public async Task EnrichAsync_WithNullMediaId_ExcludesItFromTheMediaQuery()
    {
        var mediaId = Guid.CreateVersion7();
        var withoutMedia = BuildProfile();
        var withMedia = BuildProfile(mediaId: mediaId);
        SetupMediaAssets(
            Result<List<MediaAssetSummary>>.Success(
                ApplicationStatus.Success,
                [BuildMediaAsset(mediaId)]
            )
        );

        await _enricher.EnrichAsync(
            [withoutMedia, withMedia],
            TestContext.Current.CancellationToken
        );

        var query = GetMediaQuery();

        Assert.Single(query.MediaIds);
        Assert.Equal(mediaId, query.MediaIds[0]);
        Assert.DoesNotContain(Guid.Empty, query.MediaIds);
        Assert.Null(withoutMedia.AvatarUrl);
        Assert.Equal("https://cdn.hospital.vn/avatar.png", withMedia.AvatarUrl);
    }

    [Fact]
    public async Task EnrichAsync_WithDistinctMediaIds_RequestsEachIdOnce()
    {
        var firstMediaId = Guid.CreateVersion7();
        var secondMediaId = Guid.CreateVersion7();
        var first = BuildProfile(mediaId: firstMediaId);
        var second = BuildProfile(mediaId: secondMediaId);
        SetupMediaAssets(
            Result<List<MediaAssetSummary>>.Success(
                ApplicationStatus.Success,
                [BuildMediaAsset(firstMediaId), BuildMediaAsset(secondMediaId)]
            )
        );

        await _enricher.EnrichAsync([first, second], TestContext.Current.CancellationToken);

        var query = GetMediaQuery();

        Assert.Equal(2, query.MediaIds.Length);
        Assert.Contains(firstMediaId, query.MediaIds);
        Assert.Contains(secondMediaId, query.MediaIds);
        Assert.Single(MediaQueryInvocations());
    }

    #endregion

    #region Fail path

    [Fact]
    public async Task EnrichAsync_WhenAccountLookupFails_ReturnsProfilesWithoutAccountData()
    {
        var profile = BuildProfile();
        SetupAccounts(
            Result<List<AccountSummary>>.Failure(
                new ApplicationError(ApplicationStatus.ServerError, "boom")
            )
        );

        await _enricher.EnrichAsync([profile], TestContext.Current.CancellationToken);

        Assert.Null(profile.Email);
        Assert.Null(profile.Phone);
        Assert.Null(profile.AccountRole);
        Assert.Null(profile.AccountStatus);
        Assert.Single(AccountQueryInvocations());
    }

    [Fact]
    public async Task EnrichAsync_WhenAccountLookupThrows_SwallowsTheException()
    {
        var profile = BuildProfile();
        SetupAccountsToThrow(new InvalidOperationException("bus unavailable"));

        await _enricher.EnrichAsync([profile], TestContext.Current.CancellationToken);

        Assert.Null(profile.Email);
        Assert.Null(profile.Phone);
        Assert.Null(profile.AccountRole);
        Assert.Null(profile.AccountStatus);
        Assert.Single(AccountQueryInvocations());
    }

    [Fact]
    public async Task EnrichAsync_WhenMediaLookupFails_LeavesAvatarUrlNull()
    {
        var profile = BuildProfile(mediaId: Guid.CreateVersion7());
        SetupMediaAssets(
            Result<List<MediaAssetSummary>>.Failure(
                new ApplicationError(ApplicationStatus.ServerError, "boom")
            )
        );

        await _enricher.EnrichAsync([profile], TestContext.Current.CancellationToken);

        Assert.Null(profile.AvatarUrl);
        Assert.Single(MediaQueryInvocations());
    }

    [Fact]
    public async Task EnrichAsync_WhenMediaLookupThrows_SwallowsTheException()
    {
        var profile = BuildProfile(mediaId: Guid.CreateVersion7());
        SetupMediaAssetsToThrow(new InvalidOperationException("bus unavailable"));

        await _enricher.EnrichAsync([profile], TestContext.Current.CancellationToken);

        Assert.Null(profile.AvatarUrl);
        Assert.Single(MediaQueryInvocations());
    }

    [Fact]
    public async Task EnrichAsync_WhenAccountLookupFails_StillEnrichesTheAvatar()
    {
        var accountId = Guid.CreateVersion7();
        var mediaId = Guid.CreateVersion7();
        var profile = BuildProfile(accountId: accountId, mediaId: mediaId);
        SetupAccounts(
            Result<List<AccountSummary>>.Failure(
                new ApplicationError(ApplicationStatus.ServerError, "boom")
            )
        );
        SetupMediaAssets(
            Result<List<MediaAssetSummary>>.Success(
                ApplicationStatus.Success,
                [BuildMediaAsset(mediaId)]
            )
        );

        await _enricher.EnrichAsync([profile], TestContext.Current.CancellationToken);

        Assert.Null(profile.Email);
        Assert.Null(profile.Phone);
        Assert.Null(profile.AccountRole);
        Assert.Null(profile.AccountStatus);
        Assert.Equal("https://cdn.hospital.vn/avatar.png", profile.AvatarUrl);
    }

    #endregion

    #region Bus helpers

    private void SetupAccounts(Result<List<AccountSummary>> result) =>
        _bus
            .Setup(b =>
                b.InvokeAsync<Result<List<AccountSummary>>>(
                    It.IsAny<GetAccountsByIdsQuery>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                )
            )
            .ReturnsAsync(result);

    private void SetupAccountsToThrow(Exception exception) =>
        _bus
            .Setup(b =>
                b.InvokeAsync<Result<List<AccountSummary>>>(
                    It.IsAny<GetAccountsByIdsQuery>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                )
            )
            .ThrowsAsync(exception);

    private void SetupMediaAssets(Result<List<MediaAssetSummary>> result) =>
        _bus
            .Setup(b =>
                b.InvokeAsync<Result<List<MediaAssetSummary>>>(
                    It.IsAny<GetMediaAssetsByIdsQuery>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                )
            )
            .ReturnsAsync(result);

    private void SetupMediaAssetsToThrow(Exception exception) =>
        _bus
            .Setup(b =>
                b.InvokeAsync<Result<List<MediaAssetSummary>>>(
                    It.IsAny<GetMediaAssetsByIdsQuery>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<TimeSpan?>()
                )
            )
            .ThrowsAsync(exception);

    private List<GetAccountsByIdsQuery> AccountQueryInvocations() =>
        _bus.Invocations
            .Select(x => x.Arguments[0])
            .OfType<GetAccountsByIdsQuery>()
            .ToList();

    private List<GetMediaAssetsByIdsQuery> MediaQueryInvocations() =>
        _bus.Invocations
            .Select(x => x.Arguments[0])
            .OfType<GetMediaAssetsByIdsQuery>()
            .ToList();

    private GetAccountsByIdsQuery GetAccountQuery() =>
        Assert.Single(AccountQueryInvocations());

    private GetMediaAssetsByIdsQuery GetMediaQuery() => Assert.Single(MediaQueryInvocations());

    #endregion

    #region Data helpers

    private static TestProfile BuildProfile(Guid? accountId = null, Guid? mediaId = null) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId ?? Guid.CreateVersion7(),
            Role = ProfileRole.Doctor,
            FirstName = "Nguyen",
            LastName = "Van A",
            Degrees = [DegreeType.Doctor],
            AcademicTitle = AcademicTitle.None,
            Position = PositionType.StaffDoctor,
            Gender = GenderType.Male,
            CitizenIdentificationNumber = "001088012345",
            Address = "12 Le Loi, District 1, Ho Chi Minh City",
            MediaId = mediaId,
        };

    private static AccountSummary BuildAccount(
        Guid id,
        string email = "doctor@hospital.vn",
        string phone = "+84901234567",
        string role = "Doctor",
        string status = "Active"
    ) =>
        new()
        {
            Id = id,
            Email = email,
            Phone = phone,
            Role = role,
            Status = status,
        };

    private static MediaAssetSummary BuildMediaAsset(
        Guid id,
        string? url = "https://cdn.hospital.vn/avatar.png"
    ) =>
        new()
        {
            Id = id,
            Url = url,
        };

    /// <summary>
    /// Concrete profile used as the enrichment target; the real profiles are
    /// projections of the doctor and head department doctor entities.
    /// </summary>
    private sealed record TestProfile : DoctorProfileItem { }

    #endregion
}

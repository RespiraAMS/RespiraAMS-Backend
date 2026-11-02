using Microsoft.Extensions.Logging;
using Respira.Doctor.Application.Constracts.Enrichment;
using Respira.ServiceDefaults.Contracts.Messages.Accounts;
using Respira.ServiceDefaults.Contracts.Messages.Media;
using Respira.ServiceDefaults.Contracts.Results;
using Wolverine;

namespace Respira.Doctor.Application.Features.Doctors.Common
{
    public class DoctorProfileEnricher(
        IMessageBus bus,
        ILogger<DoctorProfileEnricher> logger
    ) : IDoctorProfileEnricher
    {
        public async Task EnrichAsync(
            IEnumerable<DoctorProfileItem> profiles,
            CancellationToken cancellationToken = default
        )
        {
            // Copying the sequence keeps the reference to every item, so the
            // assignments below still update the paged result returned to the caller.
            var items = profiles.ToList();

            if (items.Count == 0)
            {
                return;
            }

            var accountIds = items.Select(x => x.AccountId).Distinct().ToArray();
            var mediaIds = items
                .Where(x => x.MediaId.HasValue)
                .Select(x => x.MediaId!.Value)
                .Distinct()
                .ToArray();

            var accounts = await LoadAccountsAsync(accountIds, cancellationToken);
            var mediaAssets = await LoadMediaAssetsAsync(mediaIds, cancellationToken);

            foreach (var item in items)
            {
                if (accounts.TryGetValue(item.AccountId, out var account))
                {
                    item.Email = account.Email;
                    item.Phone = account.Phone;
                    item.AccountRole = account.Role;
                    item.AccountStatus = account.Status;
                }

                if (
                    item.MediaId is Guid mediaId
                    && mediaAssets.TryGetValue(mediaId, out var mediaAsset)
                )
                {
                    item.AvatarUrl = mediaAsset.Url;
                }
            }
        }

        private async Task<Dictionary<Guid, AccountSummary>> LoadAccountsAsync(
            Guid[] accountIds,
            CancellationToken cancellationToken
        )
        {
            if (accountIds.Length == 0)
            {
                return [];
            }

            try
            {
                var response = await bus.InvokeAsync<Result<List<AccountSummary>>>(
                    new GetAccountsByIdsQuery { AccountIds = accountIds },
                    cancellationToken
                );

                if (response.IsSuccess() && response.Data is not null)
                {
                    return response.Data.ToDictionary(x => x.Id);
                }

                logger.LogWarning(
                    "Account lookup for doctor profiles failed with status {StatusCode}",
                    response.StatusCode
                );
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Account lookup for doctor profiles failed; returning profiles without account data"
                );
            }

            return [];
        }

        private async Task<Dictionary<Guid, MediaAssetSummary>> LoadMediaAssetsAsync(
            Guid[] mediaIds,
            CancellationToken cancellationToken
        )
        {
            if (mediaIds.Length == 0)
            {
                return [];
            }

            try
            {
                var response = await bus.InvokeAsync<Result<List<MediaAssetSummary>>>(
                    new GetMediaAssetsByIdsQuery { MediaIds = mediaIds },
                    cancellationToken
                );

                if (response.IsSuccess() && response.Data is not null)
                {
                    return response.Data.ToDictionary(x => x.Id);
                }

                logger.LogWarning(
                    "Media lookup for doctor profiles failed with status {StatusCode}",
                    response.StatusCode
                );
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Media lookup for doctor profiles failed; returning profiles without avatars"
                );
            }

            return [];
        }
    }
}

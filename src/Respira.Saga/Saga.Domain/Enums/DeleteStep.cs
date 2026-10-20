using System.Text.Json.Serialization;

namespace Respira.Saga.Domain.Enums
{
    /// <summary>
    /// The step of the profile deletion saga the orchestration is currently at (or the
    /// terminal state it ended in). The delete flow does not compensate: every step is
    /// idempotent, so a failure is reported as <see cref="Failed"/> and the client can
    /// re-send the whole flow.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DeleteStep
    {
        /// <summary>Sending the soft delete profile command to the Doctor microservice.</summary>
        DeletingProfile,

        /// <summary>Sending the soft delete account command to the Authentication microservice.</summary>
        DeletingAccount,

        /// <summary>Deleting the media asset of the avatar through the Media microservice.</summary>
        DeletingMedia,

        /// <summary>Profile, account and media asset are all gone.</summary>
        Completed,

        /// <summary>A step failed; the client may re-send the whole flow.</summary>
        Failed,
    }
}

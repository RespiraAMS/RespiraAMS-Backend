using System.Text.Json.Serialization;

namespace Respira.Saga.Domain.Enums
{
    /// <summary>
    /// The step of the profile update saga the orchestration is currently at (or the
    /// terminal state it ended in). The update flow does not compensate: any failure
    /// goes straight to <see cref="Failed"/> and the whole flow can be re-sent.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum UpdateStep
    {
        /// <summary>Sending the update account command to the Authentication microservice.</summary>
        UpdatingAccount,

        /// <summary>Uploading the new avatar to the Media microservice.</summary>
        UploadingAvatar,

        /// <summary>Sending the update profile command to the Doctor microservice.</summary>
        UpdatingProfile,

        /// <summary>Deleting the avatar that the profile no longer points at.</summary>
        DeletingOldMedia,

        /// <summary>Every step succeeded, including the old avatar cleanup.</summary>
        Completed,

        /// <summary>A step failed; the client may re-send the whole flow.</summary>
        Failed,
    }
}

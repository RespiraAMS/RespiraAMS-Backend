using System.Text.Json.Serialization;

namespace Respira.Saga.Domain.Enums
{
    /// <summary>
    /// The step of the account profile creation saga the orchestration is currently at (or the
    /// terminal state it ended in).
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CreateStep
    {
        /// <summary>Sending the create account command to the Authentication microservice.</summary>
        CreatingAccount,

        /// <summary>Sending the create profile command to the Doctor microservice.</summary>
        CreatingProfile,

        /// <summary>Uploading the avatar to the Media microservice.</summary>
        CreatingMedia,

        /// <summary>Linking the uploaded media back to the doctor/manager profile.</summary>
        LinkingMedia,

        /// <summary>Sending the verification email command to the Authentication microservice.</summary>
        SendingVerificationEmail,

        /// <summary>A step failed and the saga is rolling back what was already created.</summary>
        Compensating,

        /// <summary>Every step succeeded, including the verification email.</summary>
        Completed,

        /// <summary>The flow stopped, either because a step failed or compensation did.</summary>
        Failed,
    }
}

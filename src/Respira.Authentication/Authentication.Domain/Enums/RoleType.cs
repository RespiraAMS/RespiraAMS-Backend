using System.Text.Json.Serialization;

namespace Respira.Authentication.Domain.Enums
{
    /// <summary>
    /// User roles in the system. Determines access level and permissions.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RoleType
    {
        /// <summary>A standard doctor account.</summary>
        Doctor,

        /// <summary>
        /// A medical regulation reviewer account. This is an account-only role and
        /// never has a doctor profile.
        /// </summary>
        MedicalRegulationReviewer,

        /// <summary>An administrator account with full access.</summary>
        Admin,

        /// <summary>
        /// A head of department doctor account. This is a doctor account with the
        /// additional permission to create doctor accounts/profiles and manage the
        /// doctors under their authority.
        /// </summary>
        HeadDepartmentDoctor,
    }
}

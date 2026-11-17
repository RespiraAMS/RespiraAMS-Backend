using System.Text.Json;
using System.Text.Json.Serialization;
using Respira.Authentication.Domain.Enums;
using Respira.Doctor.Domain.Enums;

namespace Respira.Saga.API.Models
{
    /// <summary>
    /// Payload of the <c>request</c> form part sent to the profile creation endpoint. It mirrors
    /// <see cref="Application.Features.Create.Commands.StartCreateCommand"/>
    /// minus the two fields the API owns: the saga identifier and the path of the stored avatar.
    /// </summary>
    public record CreateProfileRequest
    {
        /// <summary>JSON options used to read the <c>request</c> form part.</summary>
        public static readonly JsonSerializerOptions SerializerOptions = new(
            JsonSerializerDefaults.Web
        )
        {
            Converters = { new JsonStringEnumConverter() },
        };

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        /// <summary>
        /// Account role to create. Doctor and head of department doctor roles also get a
        /// profile; a medical regulation reviewer account has no profile.
        /// </summary>
        public RoleType Role { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public List<DegreeType>? Degrees { get; set; } = [];

        public AcademicTitle? AcademicTitle { get; set; }

        public PositionType? Position { get; set; }

        public GenderType? Gender { get; set; }

        public string CitizenIdentificationNumber { get; set; } = string.Empty;

        public DateTimeOffset? DateOfBirth { get; set; }

        public string Address { get; set; } = string.Empty;
    }
}

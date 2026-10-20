using System.Text.Json;
using System.Text.Json.Serialization;
using Respira.Doctor.Domain.Enums;

namespace Respira.Saga.API.Models
{
    /// <summary>
    /// Payload of the <c>request</c> form part sent to the profile update endpoint. It
    /// mirrors <see cref="Respira.Saga.Application.Features.Update.Commands.StartUpdateCommand"/>
    /// minus the fields the API owns: the saga identifier, the two target identifiers and
    /// the path of the stored avatar.
    /// </summary>
    public record UpdateProfileRequest
    {
        /// <summary>JSON options used to read the <c>request</c> form part.</summary>
        public static readonly JsonSerializerOptions SerializerOptions = new(
            JsonSerializerDefaults.Web
        )
        {
            Converters = { new JsonStringEnumConverter() },
        };

        /// <summary>Account to update.</summary>
        public Guid AccountId { get; set; }

        /// <summary>Doctor/manager profile to update.</summary>
        public Guid DoctorId { get; set; }

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        /// <summary>New password, or null/empty to keep the current one.</summary>
        public string? Password { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public List<DegreeType> Degrees { get; set; } = [];

        public AcademicTitleEnum AcademicTitle { get; set; }

        public PositionType Position { get; set; }

        public GenderType Gender { get; set; }

        public string CitizenIdentificationNumber { get; set; } = string.Empty;

        public DateTimeOffset? DateOfBirth { get; set; }

        public string Address { get; set; } = string.Empty;
    }
}

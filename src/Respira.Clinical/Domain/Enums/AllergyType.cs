using System.Text.Json.Serialization;

namespace Respira.Clinical.Domain.Enums
{
    /// <summary>
    /// Type of allergy
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AllergyType
    {
        /// <summary>
        /// Has antibiotic allergy
        /// </summary>
        Yes,

        /// <summary>
        /// Don't have antibiotic allergy
        /// </summary>
        No,

        /// <summary>
        /// Have antibiotic allergy, but that antibiotic is not
        /// registerd in the system database
        /// </summary>
        NonRegisteredAntibiotic,

        /// <summary>
        /// Unknown allergy, which can either be non-antibiotic allergy,
        /// or simply unknown. Again, since this system only deal with
        /// antibiotic, it will be grouped together
        /// </summary>
        Unknown
    }
}

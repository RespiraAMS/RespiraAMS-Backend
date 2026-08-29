using System.Text.Json.Serialization;

namespace Domain.Enums
{
    /// <summary>
    /// Academic degrees that a doctor may hold.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DegreeType
    {
        /// <summary>Professor degree</summary>
        Professor,

        /// <summary>Doctor degree</summary>
        Doctor,

        /// <summary>Master degree</summary>
        Master,

        /// <summary>Bachelor degree</summary>
        Bachelor,

        /// <summary>Associate degree</summary>
        Associate,

        /// <summary>Undergraduate</summary>
        Undergraduate,

        /// <summary>Graduate</summary>
        Graduate,

        /// <summary>PhD degree</summary>
        PhD,

        /// <summary>Post-doctorate</summary>
        PostDoc,
    }
}

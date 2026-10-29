using System.Text.Json.Serialization;

namespace Respira.Clinical.Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MicrobialSampleType
    {
        /// <summary>
        /// Mẫu đàm (đờm)
        /// </summary>
        Sputum,

        /// <summary>
        /// Dịch rửa phế quản/phế nang
        /// </summary>
        BronchoalveolarLavageFluid,

        /// <summary>
        /// Mẫu dịch màn phổi
        /// </summary>
        PleuralFluid,

        /// <summary>
        /// Nước tiểu
        /// </summary>
        Urine,

        Blood,

        /// <summary>
        /// Dịch ổ bụng
        /// </summary>
        AbdominalFluid,

        /// <summary>
        /// Dịch não tủy
        /// </summary>
        CerebrospinalFluid,

        /// <summary>
        /// Dịch tiết từ vết thương
        /// </summary>
        WoundExudate,

        Other
    }
}

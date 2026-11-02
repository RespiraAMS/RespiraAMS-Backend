using System.Text.Json.Serialization;

namespace Respira.Clinical.Domain.Enums
{
    /// <summary>
    /// Clinical variable category. To differenciate between paraclinical
    /// and clinical, reference this:
    /// https://nhathuoclongchau.com.vn/bai-viet/kham-lam-sang-va-can-lam-sang-khac-nhau-nhu-the-nao.html
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ClinicalVariableCategory
    {
        /// <summary>
        /// Thông tin cá nhân
        /// </summary>
        PersonalInformation,

        /// <summary>
        /// Tiền sử bệnh
        /// </summary>
        MedicalHistory,

        /// <summary>
        /// Chỉ số cận lâm sàng
        /// </summary>
        Paraclinical,

        /// <summary>
        /// Chỉ số lâm sàng
        /// </summary>
        Clinical
    }
}

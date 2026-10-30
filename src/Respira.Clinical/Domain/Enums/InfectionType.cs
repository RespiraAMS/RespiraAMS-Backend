using System.Text.Json.Serialization;

namespace Respira.Clinical.Domain.Enums
{
    /// <summary>
    /// NOTE: some of these infections are already covered in some
    /// clinical variables, but we still add them here.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    /// <summary>
    /// Other active infections (Appendix 5).
    /// </summary>
    public enum InfectionType
    {
        #region Skin, soft tissue, bone & joint

        /// <summary>
        /// Nhiễm khuẩn da/mô dưới da
        /// </summary>
        SkinAndSoftTissueInfection = 1,

        /// <summary>
        /// Viêm khớp nhiễm khuẩn
        /// </summary>
        SepticArthritis = 2,

        /// <summary>
        /// Viêm tủy xương nhiễm khuẩn
        /// </summary>
        Osteomyelitis = 3,

        /// <summary>
        /// Viêm cơ/áp xe cơ nhiễm khuẩn
        /// </summary>
        BacterialMyositisOrMuscleAbscess = 4,

        /// <summary>
        /// Nhiễm khuẩn hạt tophi
        /// </summary>
        InfectedGoutyTophus = 5,

        #endregion

        #region Renal & genitourinary

        /// <summary>
        /// Viêm/áp xe thận - bể thận
        /// </summary>
        PyelonephritisOrRenalAbscess = 6,

        /// <summary>
        /// Viêm bàng quang cấp
        /// </summary>
        AcuteCystitis = 7,

        /// <summary>
        /// Viêm niệu đạo cấp không do lậu
        /// </summary>
        AcuteNonGonococcalUrethritis = 8,

        /// <summary>
        /// Viêm âm đạo niệu đạo do vi khuẩn
        /// </summary>
        BacterialVaginourethritis = 9,

        /// <summary>
        /// Viêm tử cung do vi khuẩn
        /// </summary>
        BacterialEndometritis = 10,

        #endregion

        #region Central nervous system

        /// <summary>
        /// Viêm màng não mủ
        /// </summary>
        PurulentMeningitis = 11,

        /// <summary>
        /// Viêm não do vi khuẩn
        /// </summary>
        BacterialEncephalitis = 12,

        #endregion

        #region Thoracic & cardiac

        /// <summary>
        /// Tràn mủ màng phổi
        /// </summary>
        PleuralEmpyema = 13,

        /// <summary>
        /// Áp xe trung thất
        /// </summary>
        MediastinalAbscess = 14,

        /// <summary>
        /// Viêm nội tâm mạc nhiễm khuẩn
        /// </summary>
        InfectiveEndocarditis = 15,

        /// <summary>
        /// Viêm cơ tim do vi khuẩn
        /// </summary>
        BacterialMyocarditis = 16,

        /// <summary>
        /// Viêm màng ngoài tim do vi khuẩn
        /// </summary>
        BacterialPericarditis = 17,

        #endregion

        #region Tuberculosis

        /// <summary>
        /// Lao phổi
        /// </summary>
        PulmonaryTuberculosis = 18,

        /// <summary>
        /// Lao ngoài phổi
        /// </summary>
        ExtrapulmonaryTuberculosis = 19,

        #endregion

        #region Gastrointestinal, hepatobiliary & abdominal

        /// <summary>
        /// Nhiễm trùng đường mật
        /// </summary>
        BiliaryTractInfection = 20,

        /// <summary>
        /// Áp xe gan do vi khuẩn
        /// </summary>
        BacterialLiverAbscess = 21,

        /// <summary>
        /// Viêm tụy cấp nhiễm khuẩn
        /// </summary>
        InfectedAcutePancreatitis = 22,

        /// <summary>
        /// Viêm phúc mạc nhiễm khuẩn
        /// </summary>
        BacterialPeritonitis = 23,

        /// <summary>
        /// Tiêu chảy do vi khuẩn
        /// </summary>
        BacterialDiarrhea = 24,

        /// <summary>
        /// Nhiễm Helicobacter pylori
        /// </summary>
        HelicobacterPyloriInfection = 25,

        #endregion

        #region Systemic

        /// <summary>
        /// Nhiễm trùng huyết
        /// </summary>
        Sepsis = 26,

        #endregion

        #region Ophthalmic

        /// <summary>
        /// Viêm tổ chức hốc mắt
        /// </summary>
        OrbitalCellulitis = 27,

        /// <summary>
        /// Viêm kết mạc mắt
        /// </summary>
        Conjunctivitis = 28,

        /// <summary>
        /// Viêm giác mạc mắt
        /// </summary>
        Keratitis = 29,

        /// <summary>
        /// Viêm túi lệ mắt
        /// </summary>
        Dacryocystitis = 30,

        #endregion

        #region Head & neck

        /// <summary>
        /// Áp xe vùng hàm-mặt
        /// </summary>
        MaxillofacialAbscess = 31,

        #endregion

        Other = 99
    }
}

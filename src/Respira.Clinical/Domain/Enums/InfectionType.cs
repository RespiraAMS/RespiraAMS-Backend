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
        // Skin, soft tissue, bone & joint
        SkinAndSoftTissueInfection = 1,        // Nhiễm khuẩn da/mô dưới da
        SepticArthritis = 2,                   // Viêm khớp nhiễm khuẩn
        Osteomyelitis = 3,                     // Viêm tủy xương nhiễm khuẩn
        BacterialMyositisOrMuscleAbscess = 4,  // Viêm cơ/áp xe cơ nhiễm khuẩn
        InfectedGoutyTophus = 5,               // Nhiễm khuẩn hạt tophi

        // Renal & genitourinary
        PyelonephritisOrRenalAbscess = 6,      // Viêm/áp xe thận - bể thận
        AcuteCystitis = 7,                     // Viêm bàng quang cấp
        AcuteNonGonococcalUrethritis = 8,      // Viêm niệu đạo cấp không do lậu
        BacterialVaginourethritis = 9,         // Viêm âm đạo niệu đạo do vi khuẩn
        BacterialEndometritis = 10,            // Viêm tử cung do vi khuẩn

        // Central nervous system
        PurulentMeningitis = 11,               // Viêm màng não mủ
        BacterialEncephalitis = 12,            // Viêm não do vi khuẩn

        // Thoracic & cardiac
        PleuralEmpyema = 13,                   // Tràn mủ màng phổi
        MediastinalAbscess = 14,               // Áp xe trung thất
        InfectiveEndocarditis = 15,            // Viêm nội tâm mạc nhiễm khuẩn
        BacterialMyocarditis = 16,             // Viêm cơ tim do vi khuẩn
        BacterialPericarditis = 17,            // Viêm màng ngoài tim do vi khuẩn

        // Tuberculosis
        PulmonaryTuberculosis = 18,            // Lao phổi
        ExtrapulmonaryTuberculosis = 19,       // Lao ngoài phổi

        // Gastrointestinal, hepatobiliary & abdominal
        BiliaryTractInfection = 20,            // Nhiễm trùng đường mật
        BacterialLiverAbscess = 21,            // Áp xe gan do vi khuẩn
        InfectedAcutePancreatitis = 22,        // Viêm tụy cấp nhiễm khuẩn
        BacterialPeritonitis = 23,             // Viêm phúc mạc nhiễm khuẩn
        BacterialDiarrhea = 24,                // Tiêu chảy do vi khuẩn
        HelicobacterPyloriInfection = 25,      // Nhiễm Helicobacter pylori

        // Systemic
        Sepsis = 26,                           // Nhiễm trùng huyết

        // Ophthalmic
        OrbitalCellulitis = 27,                // Viêm tổ chức hốc mắt
        Conjunctivitis = 28,                   // Viêm kết mạc mắt
        Keratitis = 29,                        // Viêm giác mạc mắt
        Dacryocystitis = 30,                   // Viêm túi lệ mắt

        // Head & neck
        MaxillofacialAbscess = 31,             // Áp xe vùng hàm-mặt

        Other = 99                             // Khác
    }
}

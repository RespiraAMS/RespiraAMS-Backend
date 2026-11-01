namespace Respira.Doctor.Domain.Entities
{
    /// <summary>A standard doctor profile.</summary>
    public class Doctor : BaseDoctor
    {
        /// <summary>Head of department doctor responsible for this doctor, if assigned.</summary>
        public Guid? HeadDepartmentDoctorId { get; set; }
    }
}

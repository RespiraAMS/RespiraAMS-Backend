namespace Respira.Doctor.Domain.Entities
{
    /// <summary>
    /// A head of department doctor profile. This is the special doctor that manages the
    /// doctors under their authority.
    /// </summary>
    public class HeadDepartmentDoctor : BaseDoctor
    {
        /// <summary>Doctors managed by this head of department doctor.</summary>
        public ICollection<Doctor> Subordinates { get; set; } = [];
    }
}

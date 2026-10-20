namespace Respira.Doctor.Domain.Entities
{
    public class Manager : BaseDoctor
    {
        public ICollection<Doctor> Subordinates { get; set; } = [];
    }
}

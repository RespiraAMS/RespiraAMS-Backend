using Microsoft.EntityFrameworkCore;
using DoctorEntity = Respira.Doctor.Domain.Entities.Doctor;

namespace Respira.Doctor.Application.Constracts.Data
{
    public interface IDoctorDbContext
    {
        DbSet<DoctorEntity> Doctors { get; }
        DbSet<Respira.Doctor.Domain.Entities.Manager> Managers { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}

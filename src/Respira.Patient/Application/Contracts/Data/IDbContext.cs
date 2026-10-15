using Microsoft.EntityFrameworkCore;
using Respira.Patient.Domain.Entities;

namespace Respira.Patient.Application.Contracts.Data
{
    public interface IDbContext
    {
        DbSet<Domain.Entities.Patient> Patients { get; set; }
        DbSet<NextOfKin> NextOfKins { get; set; }
        DbSet<Treatment> Treatments { get; set; }

        /// <summary>
        /// Save all changes to database
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The number of records changed</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute all async db operations in transaction
        /// </summary>
        /// <param name="action">An async action that wrap around all database operations</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);

        /// <summary>
        /// Execute all db operations in transaction
        /// </summary>
        /// <param name="action">An action that wrap around all database operations</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task</returns>
        Task ExecuteInTransactionAsync(Action action, CancellationToken cancellationToken = default);

        /// <summary>
        /// Dispose the context
        /// </summary>
        ValueTask DisposeAsync();
    }
}

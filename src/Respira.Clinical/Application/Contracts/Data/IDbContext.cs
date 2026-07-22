using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Respira.ServiceDefaults.Models;

namespace Application.Contracts.Data;

public interface IDbContext
{
    DbSet<Antibiogram> Antibiograms { get; }
    DbSet<Antibiotic> Antibiotics { get; }
    DbSet<AntibioticGroup> AntibioticGroups { get; }
    DbSet<Cause> Causes { get; }
    DbSet<Criterion> Criteria { get; }
    DbSet<Disease> Diseases { get; }
    DbSet<Dosage> Dosages { get; }
    DbSet<EmpiricTreatmentProtocol> EmpiricTreatmentProtocols { get; }
    DbSet<IcuHospitalizeCriterion> IcuHospitalizeCriteria { get; }
    DbSet<Pathogen> Pathogens { get; }
    DbSet<ResistanceRiskFactor> ResistanceRiskFactors { get; }

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
    /// Stubs are ONLY used to establish join-table FKs, without the need to fetch the actual data from database
    /// </summary>
    /// <param name="id">ID of the item that get attached</param>
    /// <typeparam name="T">Type of the item that get attached</typeparam>
    /// <returns>The object of type T</returns>
    T AttachStub<T>(Guid id) where T : Base;

    /// <summary>
    /// Helper method: used to bypass change tracking of EF Core when updating (adding) a list of IDs
    /// to an entity (which basically adding new records in the join table)
    /// </summary>
    /// <param name="collection">The collection of the item to be added</param>
    /// <param name="ids">The list of IDs to be added</param>
    /// <typeparam name="T">Type of the item</typeparam>
    void UpdateRelations<T>(ICollection<T> collection, IEnumerable<Guid>? ids) where T : Base;
}
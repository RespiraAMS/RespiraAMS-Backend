namespace Respira.Saga.Application.Constracts.Sagas
{
    /// <summary>
    /// Reads the persisted state of a saga run. Wolverine deletes the row as soon as a saga
    /// completes, so only in-flight runs can be observed; a miss returns
    /// <see langword="null"/>.
    /// </summary>
    public interface ISagaStateReader
    {
        /// <summary>
        /// Loads the state of the saga <typeparamref name="T"/> with the given identifier,
        /// deserialized exactly as Wolverine persisted it.
        /// </summary>
        /// <param name="sagaId">Identity of the saga run.</param>
        /// <param name="cancellationToken">Token to cancel the database read.</param>
        /// <returns>The saga state, or <see langword="null"/> when it does not exist.</returns>
        Task<T?> ReadAsync<T>(Guid sagaId, CancellationToken cancellationToken = default)
            where T : class;
    }
}

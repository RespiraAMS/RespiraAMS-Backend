namespace Application.Abstracts.Mapping
{
    /// <summary>
    /// Generic mapper interface for transforming between DTOs and domain models.
    /// </summary>
    public interface IMapper<In, Out>
    {
        /// <summary>Transforms an input of type <typeparamref name="In"/> into an <typeparamref name="Out"/>.</summary>
        /// <param name="input">The source value to transform.</param>
        /// <returns>The transformed output.</returns>
        Out Transform(In input);
    }
}

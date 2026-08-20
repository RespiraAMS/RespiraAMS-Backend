namespace Application.Abstracts.Mapping
{
    /// <summary>
    /// Interface for the mapper
    /// </summary>
    /// <typeparam name="In">The type of the input</typeparam>
    /// <typeparam name="Out">The type of the output want to transform</typeparam>
    public interface IMapper<In, Out>
    {
        Out Transform(In input);
    }
}

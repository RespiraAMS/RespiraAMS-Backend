namespace Application.Abstracts.Mapping
{
    /// <summary>
    /// Generic mapper interface for transforming between DTOs and domain models.
    /// </summary>
    public interface IMapper<In, Out>
    {
        Out Transform(In input);
    }
}

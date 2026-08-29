namespace Application.Contracts.Mappers;

/// <summary>
/// Generic mapper interface
/// </summary>
/// <typeparam name="TSource">source type</typeparam>
/// <typeparam name="TDest">destination type</typeparam>
public interface IMapper<in TSource, out TDest>
{
    TDest Map(TSource source);
}

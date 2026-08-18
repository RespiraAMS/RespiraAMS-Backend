namespace Application.Contracts.Mappers;

public interface IMapper<in TSource, out TDest>
{
    TDest Map(TSource source);
}

namespace Application.Contracts.Mappers;

public interface ICreateMapper<out TModel, in TCreateCommand>
{
    TModel ToModel(TCreateCommand command);
}
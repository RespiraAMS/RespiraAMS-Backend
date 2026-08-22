namespace Application.Contracts.Mappers;

public interface IUpdateMapper<in TModel, in TUpdateCommand>
{
    void MapModel(TModel model, TUpdateCommand command);
}
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Contracts.Mappers
{
    public interface IUpdateMapper<in TModel, in TUpdateCommand>
    {
        Result MapModel(TModel model, TUpdateCommand command);
        Result MapModel(TModel model, TUpdateCommand command, object? dependencies = null);
    }
}

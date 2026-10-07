using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Clinical.Application.Contracts.Mappers
{
    public interface ICreateMapper<TCreateCommand, TModel>
    {
        Result<TModel> ToModel(TCreateCommand command);
        Result<TModel> ToModel(TCreateCommand command, object? dependencies = null);
    }
}

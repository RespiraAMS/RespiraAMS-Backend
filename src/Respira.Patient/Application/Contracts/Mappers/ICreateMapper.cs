using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Contracts.Mappers
{
    public interface ICreateMapper<TCreateCommand, TModel>
    {
        Result<TModel> ToModel(TCreateCommand command);
    }

}

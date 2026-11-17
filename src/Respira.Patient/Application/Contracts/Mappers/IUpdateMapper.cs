using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Patient.Application.Contracts.Mappers
{
    public interface IUpdateMapper<in TModel, in TUpdateCommand>
    {
        Result MapModel(TModel model, TUpdateCommand command);
    }
}

using Respira.Doctor.Application.Features.Doctors.Queries.GetById.Result;
using Respira.ServiceDefaults.Contracts.CQRS;
using Respira.ServiceDefaults.Contracts.Results;

namespace Respira.Doctor.Application.Features.Doctors.Queries.GetByAccountId
{
    public record GetDoctorByAccountIdQuery : IQuery
    {
        public Guid AccountId { get; set; }
    }
}

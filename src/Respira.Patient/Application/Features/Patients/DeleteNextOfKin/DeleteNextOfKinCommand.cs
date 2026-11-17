using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Patient.Application.Features.Patients.DeleteNextOfKin
{
    public record DeleteNextOfKinCommand : ICommand
    {
        /// <summary>
        /// Next of kin ID
        /// </summary>
        public required Guid Id { get; set; }
    }
}

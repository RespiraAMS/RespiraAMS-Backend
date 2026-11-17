using Respira.Patient.Domain.Enums;
using Respira.ServiceDefaults.Contracts.CQRS;

namespace Respira.Patient.Application.Features.Patients.DischargePatient
{
    public record DischargePatientCommand : ICommand
    {
        /// <summary>
        /// Patient ID
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// Patient's treatment outcome. Note that <see cref="TreatmentOutcome.OutOfScope"/>
        /// is a valid outcome for discharge
        /// </summary>
        public required TreatmentOutcome Outcome { get; set; }
    }
}

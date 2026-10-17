using Respira.Patient.Application.Features.Patients.GetPagedPatient;
using Respira.Patient.Application.Features.Patients.SearchPatient;
using Respira.Patient.Application.Features.Patients.UpdatePatient;
using Respira.Patient.Domain.Enums;
using Respira.ServiceDefaults.Contracts.Pagination;

namespace Respira.Patient.API.Dtos
{
    public record GetPagedPatientRequestDto
    {
        /// <summary>
        /// Pagination parameter: page index
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Pagination parameter: page size
        /// </summary>
        public int Size { get; set; } = 10;

        /// <summary>
        /// Patient gender
        /// </summary>
        public Gender? Gender { get; set; }

        /// <summary>
        /// Minimum age of patient
        /// </summary>
        public int? MinAge { get; set; }

        /// <summary>
        /// Maximum age of patient
        /// </summary>
        public int? MaxAge { get; set; }

        public GetPagedPatientQuery ToQuery()
        {
            return new GetPagedPatientQuery
            {
                Param = new PaginationParam
                {
                    Page = Page,
                    Size = Size,
                },
                Filter = new PatientFilter
                {
                    Gender = Gender,
                    MinAge = MinAge,
                    MaxAge = MaxAge,
                }
            };
        }
    }

    public record SearchPatientRequestDto
    {
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 10;
        public string SearchTerm { get; set; } = string.Empty;

        public SearchPatientQuery ToQuery()
        {
            return new SearchPatientQuery
            {
                Param = new PaginationParam
                {
                    Page = Page,
                    Size = Size,
                },
                SearchTerm = SearchTerm,
            };
        }
    }

    public record UpdatePatientRequestDto
    {
        /// <summary>
        /// Patient first name
        /// </summary>
        public required string FirstName { get; set; }

        /// <summary>
        /// Patient last name
        /// </summary>
        public required string LastName { get; set; }

        /// <summary>
        /// Patient's phone number. Should conform to Vietnamese format:<br/>
        /// 1. 03/05/07/08/09xxxxxxxx (accordding to the 2018 migrations from 11 digits to 10)<br/>
        /// 2. The internation format (84[3/5/7/8/9]xxxxxxxx or +84[3/5/7/8/9]xxxxxxxx)<br/>
        /// </summary>
        public required string PhoneNumber { get; set; }

        /// <summary>
        /// Patient's date of birth. As per business rule: only accept patient
        /// above 15. CAP for children won't be covered by this system
        /// </summary>
        public required DateOnly DateOfBirth { get; set; }

        /// <summary>
        /// Patient's gender
        /// </summary>
        public required Gender Gender { get; set; }

        /// <summary>
        /// Patient's height, calculated in centimeter
        /// </summary>
        public required decimal Height { get; set; }

        /// <summary>
        /// Patient's weight, calculated in kilogram
        /// </summary>
        public required decimal Weight { get; set; }

        /// <summary>
        /// Patient's medical record code.
        /// </summary>
        public required string MedicalRecordCode { get; set; }

        /// <summary>
        /// Patient's health insurance card number. This is the Vietnamese
        /// new health insurance card number, which consist of 10 numeric digits
        /// </summary>
        public required string HealthInsuranceCard { get; set; }

        /// <summary>
        /// Patient's address
        /// </summary>
        public required string Address { get; set; }

        /// <summary>
        /// Patient's city
        /// </summary>
        public required string City { get; set; }

        public UpdatePatientCommand ToCommand(Guid id)
        {
            return new UpdatePatientCommand
            {
                Id = id,
                FirstName = FirstName,
                LastName = LastName,
                PhoneNumber = PhoneNumber,
                DateOfBirth = DateOfBirth,
                Gender = Gender,
                Height = Height,
                Weight = Weight,
                MedicalRecordCode = MedicalRecordCode,
                HealthInsuranceCard = HealthInsuranceCard,
                Address = Address,
                City = City,
            };
        }
    }
}

using Authentication.Domain.Enums;

namespace Respira.Saga.Application.Features.Create.Commands
{
    /// <summary>
    /// Creation permissions between staff roles:
    /// <list type="bullet">
    /// <item><description>A head of department doctor can create doctor accounts/profiles.</description></item>
    /// <item><description>A medical regulation reviewer cannot create any account.</description></item>
    /// <item><description>An administrator can create doctor, medical regulation reviewer and head of department doctor accounts.</description></item>
    /// </list>
    /// A medical regulation reviewer is an account-only role: it never gets a doctor profile.
    /// </summary>
    public static class ProfileCreationPolicy
    {
        /// <summary>Checks whether the caller may create an account of the target role.</summary>
        /// <param name="callerRole">Role of the authenticated caller.</param>
        /// <param name="targetRole">Role of the account to create.</param>
        /// <returns>True when the caller is allowed to create the target account.</returns>
        public static bool CanCreate(RoleType callerRole, RoleType targetRole)
        {
            // Admin accounts are provisioned out-of-band, never through this flow.
            if (targetRole == RoleType.Admin)
            {
                return false;
            }

            return callerRole switch
            {
                RoleType.Admin => targetRole
                    is RoleType.MedicalRegulationReviewer
                        or RoleType.HeadDepartmentDoctor
                        or RoleType.Doctor,
                RoleType.HeadDepartmentDoctor => targetRole == RoleType.Doctor,
                _ => false,
            };
        }
    }
}

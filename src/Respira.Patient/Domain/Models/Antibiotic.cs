namespace Respira.Patient.Domain.Models
{
    /// <summary>
    /// This is the simplify model of antibiotic
    /// </summary>
    /// <param name="Id">Antibiotic ID</param>
    /// <param name="Name">Antibiotic name</param>
    /// <param name="Classification">Antibiotic classification</param>
    public record Antibiotic(Guid Id, string Name, string Classification);
}

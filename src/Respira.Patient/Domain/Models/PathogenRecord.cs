namespace Domain.Models;

/// <summary>
/// An immutable record of pathogen
/// </summary>
/// <param name="Id">Pathogen ID</param>
/// <param name="Name">Pathogen name</param>
public record PathogenRecord(Guid Id, string Name);


namespace Respira.ServiceDefaults.Exceptions;

/// <summary>
/// Client side error, where the entity with the identifier is not found
/// </summary>
/// <param name="entity">Entity name</param>
/// <param name="id">Identifier</param>
public class NotFoundException(string entity, Guid id) : Exception($"{entity} with this ID not found: {id}")
{
    
}
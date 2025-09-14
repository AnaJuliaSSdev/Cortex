namespace Cortex.Exceptions;

public class EntityNotFoundException(string entity): Exception($"Entity {entity} not found.");

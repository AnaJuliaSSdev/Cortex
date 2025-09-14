namespace Cortex.Exceptions;

public class EmailAlreadyInUseException(string message = "Email is already in use.") : Exception(message);
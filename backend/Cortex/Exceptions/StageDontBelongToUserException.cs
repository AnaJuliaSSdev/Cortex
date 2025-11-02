namespace Cortex.Exceptions;

public class StageDontBelongToUserException(string message = "Stage don't belong to user.") : Exception(message);

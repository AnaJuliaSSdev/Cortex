namespace Cortex.Exceptions;

public class AnalysisDontBelongToUserException(string message = "Analysis don't belong to user.") : Exception(message);

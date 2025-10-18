namespace Cortex.Exceptions;

public class AnalysisAlreadyCompletedException(string message = "Analysis already completed.") : Exception(message);

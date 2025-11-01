namespace Cortex.Services.ServicosTopicos
{
    public interface IRagService
    {
        Task<string> AskQuestionAsync(string question, int documentId);
    }
}

namespace Cortex.Services.Interfaces;

public interface IChunkService
{
    List<string> SplitIntoChunks(string text, int chunkSize = 1000, int overlap = 200);
}

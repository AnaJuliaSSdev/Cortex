using Cortex.Services.Interfaces;
using System.Text.RegularExpressions;

namespace Cortex.Services;

public class ChunkService : IChunkService
{
    public List<string> SplitIntoChunks(string text, int chunkSize = 1000, int overlap = 200)
    {
        List<string> chunks = [];

        text = Regex.Replace(text, @"\s+", " ").Trim();

        if (text.Length <= chunkSize)
        {
            chunks.Add(text);
            return chunks;
        }

        int start = 0;

        while (start < text.Length)
        {
            int end = Math.Min(start + chunkSize, text.Length);

            var chunk = text[start..end];
            chunks.Add(chunk);

            start += chunkSize - overlap;

            if (start >= text.Length)
            {
                break;
            }
        }

        return chunks;
    }
}

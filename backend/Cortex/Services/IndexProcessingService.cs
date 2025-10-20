using Cortex.Models;
using Cortex.Services.Interfaces;
using System.Text.Json;

namespace Cortex.Services;

public class IndexProcessingService : IIndexProcessingService
{

    //AQUI, ESSSE CANDIDATES NÃO ESTA VINDO PREENCHIDOS
    //QUANDO USA   ResponseMimeType = MediaTypeNames.Application.Json, ELE VOLTA SÓ O JSON Q PEDI
    //MAS SE TIRA ESSA CONFIG, ELE ENVIA SÓ TEXTO, AI EU TENHO QUE TRADUZIR PRA JSON, 
    // E AI NESSE FORMATO DE TEXTO ELE TRAS AS REFERENCIAS
    // QUANDO TIRA O JSON TB, ELE CHEGA A VIR COM O CANDIDATES PREENCHIDO, MAS AI VEM COM UMAS COISAS NADA A VER
    // NÃO É BEM OQ EU QUERO NESSE CASO
    // TALVEZ O MELHOR SEJA PEDIR MSM A REFERENCIA DE ONDE QUER

    public async Task<List<Models.Index>> ProcessGeminiResponseAsync(GeminiResponse geminiResponse, int analysisId)
    {
         if (!geminiResponse.IsSuccess || geminiResponse.FullResponse == null)
            throw new InvalidOperationException("A resposta do Gemini não foi bem-sucedida ou está vazia.");

        string jsonContent = geminiResponse.Content;
        JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
        GeminiIndicesResponse deserializedResponse = JsonSerializer.Deserialize<GeminiIndicesResponse>(jsonContent, options);

        if (deserializedResponse?.Indices == null)
        {
            throw new JsonException("Falha ao deserializar a resposta JSON do Gemini ou a lista de índices está vazia.");
        }

        // 2. Extrair a metadata de citação da resposta completa
        var citationMetadata = geminiResponse.Candidates?.FirstOrDefault()?.CitationMetadata;

        var newIndices = new List<Cortex.Models.Index>();

        // 3. Iterar sobre os DTOs e mapear para os modelos de domínio
        foreach (var indexDto in deserializedResponse.Indices)
        {
            var newIndex = new Cortex.Models.Index
            {
                Name = indexDto.Name,
                Description = indexDto.Description,
                AnalysisId = analysisId,
                Indicator = new Indicator { Name = indexDto.IndicatorName } // Cria o indicador aninhado
            };

            // 4. Vincular as citações ao índice
            if (citationMetadata?.CitationSources != null)
            {
                foreach (var source in citationMetadata.CitationSources)
                {
                    newIndex.References.Add(new IndexReference
                    {
                        SourceDocumentUri = source.Uri,
                        StartIndex = source.StartIndex,
                        EndIndex = source.EndIndex,
                        // QuotedContent = ... (se a API preencher isso ou se você extrair manualmente)
                    });
                }
            }

            newIndices.Add(newIndex);
        }

        return newIndices;
    }
}

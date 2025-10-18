using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;
using System.Text;

namespace Cortex.Services
{
    public  class PreAnalysisStageService(IDocumentRepository documentRepository) : AStageService(documentRepository)
    {
        public override async Task<AnalysisExecutionResult> ExecuteStageAsync(Analysis analysis)
        {
            AnalysisExecutionResult resultBaseClass = await base.ExecuteStageAsync(analysis); // pega os documentos e embeddings
            string promptStage = GetPromptStageAsync();
            string documentsNamesAnalysis = string.Join(",\n",
                resultBaseClass.AnalysisDocuments.Select(d => d.FileName));

            string documentsNamesReferences = string.Join(",\n",
                resultBaseClass.ReferenceDocuments.Select(d => d.FileName));

            string finalPrompt = String.Format(promptStage, analysis.Question, documentsNamesAnalysis, documentsNamesReferences);
            Console.WriteLine("--- PROMPT FINAL ENVIADO ---");
            Console.WriteLine(finalPrompt);

            //anexar os documentos de fato no prompt

            // já tem um serviço que devolve o documento, _fileStorageService..GetFileAsync(document.FilePath)

            //executar prompt, tem um serviço que executa o prompt mas ele não adiciona documentos, preciso de ajuda
            // pra refatorar
            //
            //posteriormente vou tratar a resposta

            return resultBaseClass;
        }

        public override string GetPromptStageAsync()
        {
            string promptPreAnalysis = """
            Você está na etapa de PRÉ ANÁLISE e irá fazer o LEVANTAMENTO DE ÍNDICES dos documentos de análise, segundo a metodologia de Laurance Bardin.
            A partir das entrevistas transcritas anexadas e dos documentos anexados de contextualização da etapa que estamos prestes a realizar, selecione os *índices* que compõem esses documentos de análise.
            Os documentos de contextualização devem ser utilizados como fontes de conceitos teóricos para embasar a análise.
            O foco central desta análise de conteúdo é responder a pergunta que move a análise, a pergunta central da pesquisa. 
            Você está na etapa de pré análise da metodologia e está extraindo os índices dos documentos de análise, que são entrevistas transcritas. 
            Sua resposta DEVE ser obrigatoriamente um único bloco de código JSON, sem formatação Markdown, comentários ou texto introdutório. 
            O JSON deve ser um objeto contendo uma única chave "indices", que é uma lista de objetos.
            Além disso, os trechos que originaram os índices deverão ser linkados no CitationMetadata. 

            Cada objeto na lista deve seguir exatamente esta estrutura:
            {
              "name": "Nome do Conceito",
              "description": "Uma descrição detalhada do conceito, citando diretamente as informações dos documentos."
            }

            Exemplo da estrutura completa esperada:
            {
              "indices": [
                {
                  "name": "Threat Modeling",
                  "description": "Threat Modeling é o processo de identificar, comunicar e compreender ameaças e mitigações em um contexto de proteção de algo de valor."
                },
                {
                  "name": "Input Validation",
                  "description": "A validação de entrada previne que dados malformados entrem no sistema, sendo uma defesa crucial contra ataques de injeção."
                }
              ]
            }

            Você irá receber todos os dados de contextualizãção e deverá preencher o json com os índices, cada índice deve conter um nome, e uma descrição. A descrição sobre o índice deve ser breve e conter um comentário sobre a escolha do índice e/ou o que ele representa na análise.

            PERGUNTA CENTRAL DA PESQUISA:
            {0}

            DOCUMENTOS DE CONTEXTUALIZAÇÃO SOBRE A ETAPA SEGUNDO A METODOLOGIA DE BARDIN (nomes):
            {1}

            DOCUMENTOS DE CONTEXTUALIZAÇÃO ENVIADOS (nomes): 
            {2}

            DOCUMENTOS DE ANÁLISE ENVIADOS (nomes):
            {3}
            """;

            string promptStart = base.GetPromptStageAsync();
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine(promptStart);
            
            stringBuilder.AppendLine(promptPreAnalysis);
            return stringBuilder.ToString();
        }
    }
}

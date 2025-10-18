using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;

namespace Cortex.Services;

public class ExplorationOfMaterialStageService(IDocumentRepository documentRepository) : AStageService(documentRepository)
{
    public Task<AnalysisExecutionResult> ExecuteStageAsync(Analysis analysis)
    {
        //buscar os documentos daquela análise
        //buscar os embeddings do referencial teórico e calcular a proximidade deles com a pergunta
        //trazer os dois pro prompt

        throw new NotImplementedException();
    }

    //public string GetPromptStageAsync()
    //{
    //    return $@"
    //        Você é um assistente de análise de conteúdo. Você deve se basear na análise de conteúdo segundo conceitos de 
    //        Alice Bardin. Siga essas definições de etapa:
        
    //        DEFINIÇÕES DAS ETAPAS ALICE BARDIN:
    //        [DEFINIÇÕES]

    //        Retorne APENAS um objeto JSON válido, sem texto adicional.
    //        IMPORTANTE: Retorne APENAS um objeto JSON válido, sem texto adicional, 
    //        sem explicações, sem markdown, sem blocos de código. 
    //        Comece sua resposta com {{ e termine com }}.
            
    //        DOCUMENTOS:
    //        [Entrevistas]
            
    //        QUESTÃO DE PESQUISA:
    //        [Pergunta]

    //        Realize a pré análise do conteúdo e retorne  o seguinte formato JSON:
    //        {{
    //          ""etapa"": ""pre_analise_leitura_flutuante"",
    //          ""timestamp"": ""ISO 8601 timestamp"",
    //          ""temas_emergentes"": [
    //            {{
    //              ""tema"": ""string"",
    //              ""descricao"": ""string"",
    //              ""frequencia_estimada"": ""alta|media|baixa"",
    //              ""exemplos"": [""string""]
    //            }}
    //          ],
    //          ""padroes_identificados"": [
    //            {{
    //              ""padrao"": ""string"",
    //              ""descricao"": ""string"",
    //              ""evidencias"": [""string""]
    //            }}
    //          ],
    //          ""observacoes_gerais"": ""string"", 
    //        ""hipoteses"": [
    //            {{
    //              ""id"": ""H1"",
    //              ""enunciado"": ""string"",
    //              ""justificativa"": ""string"",
    //              ""evidencias_preliminares"": [""string""],
    //              ""relacao_com_teoria"": ""string"",
    //              ""testabilidade"": ""alta|media|baixa""
    //            }}
    //          ],
    //             ""unidades_registro"": {{
    //            ""tipo"": ""palavra|frase|paragrafo|tema|objeto|personagem"",
    //            ""descricao"": ""string"",
    //            ""exemplos"": [""string""]
    //          }},
    //          ""unidades_contexto"": {{
    //            ""tipo"": ""frase|paragrafo|secao|documento"",
    //            ""descricao"": ""string""
    //          }},
    //          ""indices"": [
    //            {{
    //              ""nome"": ""string"",
    //              ""definicao"": ""string"",
    //              ""tipo"": ""palavra_chave|tema|conceito|expressao"",
    //              ""termos_relacionados"": [""string""]
    //            }}
    //          ],
    //          ""indicadores"": [
    //            {{
    //              ""nome"": ""string"",
    //              ""tipo"": ""frequencia|presenca_ausencia|coocorrencia|intensidade|direcao"",
    //              ""descricao"": ""string"",
    //              ""forma_medicao"": ""string""
    //            }}
    //          ]
    //        }}
            
    //    ";
    //}

    public override string GetPromptStageAsync()
    {
        throw new NotImplementedException();
    }
}

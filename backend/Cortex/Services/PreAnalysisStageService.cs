using Cortex.Helpers;
using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;
using GenerativeAI.Types;
using System.Text;
using static GeminiService.Api.Services.Implementations.GeminiService;

namespace Cortex.Services
{
    public  class PreAnalysisStageService(IDocumentRepository documentRepository, IGeminiService geminiService,
        ILogger<PreAnalysisStageService> logger,
        IDocumentService documentService, IIndexProcessingService indexProcessingService, IFileStorageService fileStorageService) : AStageService(documentRepository)
    {
        private readonly IGeminiService _geminiService = geminiService;
        private readonly IDocumentService _documentService = documentService;
        private readonly IIndexProcessingService _indexProcessingService = indexProcessingService;
        private readonly IFileStorageService _fileStorageService = fileStorageService;
        private readonly ILogger _logger = logger;
        #region Prompt Pre Analysis
        private const string _promptPreAnalysis = """              
            Você está na etapa de PRÉ ANÁLISE e irá fazer o LEVANTAMENTO DE ÍNDICES E A SELEÇÃO DOS INDICADORES dos documentos de análise, segundo a metodologia de Laurance Bardin.
            A partir das entrevistas transcritas anexadas e dos documentos anexados de contextualização da etapa que estamos prestes a realizar, selecione os *índices* e defina os *indicadores* que compõem esses documentos de análise.
            Os documentos de contextualização devem ser utilizados como fontes de conceitos teóricos para embasar a análise.
            O foco central desta análise de conteúdo é responder a pergunta que move a análise, a pergunta central da pesquisa. Pode não ser necessariamente uma pergunta, mas uma tese, uma motivação de pesquisa. 
            Você está na etapa de pré análise da metodologia e está extraindo os índices e indicadores dos documentos de análise, que são entrevistas transcritas. 
            Sua resposta DEVE ser obrigatoriamente um único bloco de código JSON, sem formatação Markdown, comentários ou texto introdutório. 
            O JSON deve ser um objeto contendo uma única chave "indices", que é uma lista de objetos.
            A API deverá retornar a CitationMetadata (contendo uma ou várias citações) correspondente a cada trecho utilizado para gerar cada índice.  
            Inclua informações precisas e referenciáveis. 


            Cada objeto na lista deve seguir exatamente esta estrutura:
            {{
              "name": "Nome do Conceito",
              "description": "Uma descrição detalhada do conceito, citando diretamente as informações dos documentos.",
              "indicator": "Indicador concreto para medir a presença ou a falta do índice em questão"
            }}

            Exemplo da estrutura completa esperada:
            {{
              "indices": [
                {{
                  "name": "Threat Modeling",
                  "description": "Threat Modeling é o processo de identificar, comunicar e compreender ameaças e mitigações em um contexto de proteção de algo de valor.",
                  "indicator": "A presença da palavra Threat no texto."
                }},
                {{
                  "name": "Input Validation",
                  "description": "A validação de entrada previne que dados malformados entrem no sistema, sendo uma defesa crucial contra ataques de injeção.",
                  "indicator": "A não menção de validação de input durante uma entrevista."
                }}
              ]
            }}

            Você irá receber todos os dados de contextualizãção e deverá preencher o json com os índices, cada índice deve conter um nome, e uma descrição. A descrição sobre o índice deve ser breve e conter um comentário sobre a escolha do índice e/ou o que ele representa na análise.

            PERGUNTA CENTRAL DA PESQUISA:
            {0}

            DOCUMENTOS DE CONTEXTUALIZAÇÃO ENVIADOS (nomes): 
            {1}

            DOCUMENTOS DE ANÁLISE ENVIADOS (nomes):
            {2}

            REFERENCIAL TEÓRICO ADICIONAL SOBRE A COLETA DOS ÍNDICES E INDICADORES(trecho retirado do livro de Bardin):

            Se se considerarem os textos uma manifestação que contém índices que a análise explicitará,
            o trabalho preparatório será o da escolha destes - em função das hipóteses, caso
            elas estejam determinadas - e sua organização sistemática em indicadores.
            Outro exemplo, o índice pode ser a menção explícita de um tema numa
            mensagem. Caso parta do princípio de que este tema possui tanto mais 
            importância para o locutor quanto mais frequentemente é repetido 
            (caso da análise sistemática quantitativa), o indicador correspondente será a frequência
            deste tema de maneira relativa ou absoluta, relativo a outros.
            Por exemplo: supõe-se que a emoção e a ansiedade se manifestam por perturbações da palavra durante uma entrevista terapêutica. Os índices retidos"
            ("hã", frases interrompidas, repetição, gagueira, sons incoerentes ...) e 
            a sua frequência de aparição vão servir de indicador do estado emocional subjacente.
            Uma vez escolhidos os índices, procede-se à construção de indicadores
            precisos e seguros. Desde a pré-análise devem ser determinadas operações de
            recorte do texto em unidades comparáveis de categorização para análise tem ática e de modalidade de codificação para o registro dos dados.
            Geralmente, certificamo-nos da eficácia e da pertinência dos indicadores testando-os em algumas passagens ou em alguns elementos dos documentos (pré-teste de análise).
            [...]
            Somente os índices é que são retidos de maneira
            não frequencial, podendo o analista recorrer a testes quantitativos: por exemplo, a aparição de índices similares em discursos semelhantes.        
            [...]
            A intenção da análise de conteúdo é a inferência de conhecimentos relativos às condições de produção (ou, eventualmente, de recepção), inferência esta
            que recorre a indicadores (quantitativos ou não).
            [...]
            Suponhamos um exemplo: pretendo medir o grau de ansiedade de um
            sujeito - não expresso por ele conscientemente na mensagem que emitiu -
            exigindo isto, a posteriori, uma transcrição escrita da palavra verbal e mani-
            pulações várias. Posso decidir-me pela adoção de um indicador de natureza
            semântica. Por exemplo (ao nível dos significados), anotar a frequência dos
            termos ou dos temas relativos à ansiedade, no vocabulário do sujeito. Ou
            então posso servir-me, se isso me parecer válido, de um indicador linguísti-
            co (ordem de sucessão dos elementos significantes, extensão das "frases"),
            ou paralinguístico (entoação e pausas).
            Definitivamente, o terreno, o funcionamento e o objetivo da análise de
            conteúdo podem resumir-se da seguinte maneira: atualmente, e de modo ge-
            ral, designa-se sob o termo de análise de conteúdo:
            Um conjunto de técnicas de análise das comunicações visando obter por pro-
            cedimentos sistemáticos e objetivos de descrição do conteúdo das mensagens indica-
            dores (quantitativos ou não) que permitam a inferência de conhecimentos relativos
            às condições de produção/recepção (variáveis inferidas) dessas mensagens.

            REUSMO TEÓRICO SOBRE A ETAPA:
            Ainda, nessa fase, são elaborados indicadores que fundamentam a interpretação final (BARDIN , 2011).  Se considerarmos que os documentos (o corpus) são uma manifestação que contém "vestígios" ou índices que a análise fará “falar”, a missão desta etapa é escolher esses índices e organizá-los em indicadores de forma sistemática. 
            índices (vestígios) são os sinais na superfície do texto ou da comunicação que sugerem a presença de algo que você quer investigar. Já os indicadores (medidas) são o  modo como você vai medir ou quantificar (ou não) a presença desses índices para que possa, então, inferir conhecimento sobre o contexto (a variável inferida).
            Imagine que sua hipótese seja: A emoção e a ansiedade do paciente se manifestam quando ele fala durante uma entrevista terapêutica. Neste exemplo, você transforma o fenômeno abstrato ("ansiedade") em um indicador concreto (a "contagem da frequência" dos sons incoerentes), permitindo que a análise seja objetiva e sistemática, indo além do que o paciente disse explicitamente. 
            
            Conceito a Ser Inferido (Variável Inferida) = Ansiedade/Emoção
            Hipótese = A ansiedade se manifesta por perturbações na fala
            Índices (Vestígios no Texto) = Frases interrompidas, repetição de palavras, gagueira, sons incoerentes (ex.: "hã", "hum")
            Indicador (Medida Sistemática) = A frequência de aparição desses índices
            """;
        #endregion

        public override async Task<AnalysisExecutionResult> ExecuteStageAsync(Analysis analysis)
        {
            AnalysisExecutionResult resultBaseClass = await base.ExecuteStageAsync(analysis); // pega os documentos e embeddings         
            IEnumerable<Cortex.Models.Document> allDocuments = resultBaseClass.ReferenceDocuments.Concat(resultBaseClass.AnalysisDocuments);
            //SE DER ERRADO EXCLUI OQ TA NO MEIO DO TRECHO COMENTADO E DEIXA OQ TA COMENTADO
            //List<Part> fileParts = await _documentService.ConvertDocumentsToPart(allDocuments);

            List<Part> fileParts = new();
            foreach (var document in allDocuments)
            {
                try
                {
                    byte[] fileBytes = await _fileStorageService.GetFileAsync(document.FilePath);

                    // Envia para a File API do Gemini
                    FileDetails? uploadedFile = await _geminiService.UploadFileWithHttpAsync(
                        fileBytes,
                        document.FileType.ToMimeType(),
                        document.FileName
                    );

                    if (uploadedFile is not null)
                    {
                        // Adiciona o arquivo ao prompt (via URI)
                        fileParts.Add(new Part
                        {
                            FileData = new FileData
                            {
                                MimeType = uploadedFile.MimeType,
                                FileUri = uploadedFile.Uri
                            }
                        });

                        _logger.LogInformation(
                            "Arquivo '{FileName}' adicionado ao prompt. URI: {Uri}",
                            document.FileName,
                            uploadedFile.Uri
                        );
                    }
                    else
                    {
                        _logger.LogWarning("Falha no upload do arquivo '{FileName}'. Será ignorado na análise.", document.FileName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao processar documento '{FileName}'", document.FileName);
                }
            }

            if (fileParts.Count > 0)
            {
                _logger.LogInformation("Enviando {Count} arquivos para a API Gemini...", fileParts.Count);

                GeminiResponse geminiResponse = await _geminiService.GenerateContentAsync(
                    base.CreateFinalPrompt(resultBaseClass, analysis),
                    fileParts
                );

                if (geminiResponse.IsSuccess)
                {
                    _logger.LogInformation("Resposta do Gemini recebida com sucesso.");

                    // Processa a resposta (ex: extrai citações, índices, etc.)
                    List<Cortex.Models.Index> processedIndices =
                        await _indexProcessingService.ProcessGeminiResponseAsync(geminiResponse, analysis.Id);

                    _logger.LogInformation("{Count} índices processados e vinculados com suas referências.", processedIndices.Count);

                    // Apenas um exemplo de log mais detalhado
                    var firstIndex = processedIndices.FirstOrDefault();
                    if (firstIndex is not null)
                    {
                        Console.WriteLine($"Exemplo - Nome: {firstIndex.Name}");
                        Console.WriteLine($"Indicador: {firstIndex.Indicator.Name}");
                        foreach (var reference in firstIndex.References)
                            Console.WriteLine($"  - Referência: {reference.SourceDocumentUri}");
                    }
                }
                else
                {
                    throw new Exception($"Erro da API Gemini: {geminiResponse.ErrorMessage}");
                }
            }
            else
            {
                _logger.LogWarning("Nenhum arquivo válido foi enviado à API Gemini.");
            }

            //if (fileParts.Count != 0)
            //{
            //    GeminiResponse geminiResponse = await _geminiService.GenerateContentAsync(
            //        base.CreateFinalPrompt(resultBaseClass, analysis),
            //        fileParts
            //    );

            //    if (geminiResponse.IsSuccess)
            //    {
            //        List<Cortex.Models.Index> processedIndices = await _indexProcessingService.ProcessGeminiResponseAsync(geminiResponse, analysis.Id);
            //        Console.WriteLine($"{processedIndices.Count} índices foram processados e vinculados com suas referências.");
            //        var firstIndex = processedIndices.FirstOrDefault();
            //        if (firstIndex != null)
            //        {
            //            Console.WriteLine($"Exemplo - Nome: {firstIndex.Name}");
            //            Console.WriteLine($"Indicador: {firstIndex.Indicator.Name}");
            //            foreach (var reference in firstIndex.References)
            //            {
            //                Console.WriteLine($"  - Referência: {reference.SourceDocumentUri}");
            //            }
            //        }
            //    }
            //    else
            //    {
            //        throw new Exception($"Erro da API Gemini: {geminiResponse.ErrorMessage}"); // aqui poderia tratar tb erro de too many requests
            //    }
            //}

            //- Falta salvar os indices e indicadores no banco de dados e salvar o contexto enfim retornado pelo Gemini para 'concluir' a pré análise, além de verificar essa questão de tamanho de arquivos (permitir mais de 20MB ou então usar técnica RAG)

            return resultBaseClass;
        }

        public override string GetPromptStageAsync()
        {
           string promptStart = base.GetPromptStageAsync();

            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine(promptStart);
            stringBuilder.AppendLine(_promptPreAnalysis);

            return stringBuilder.ToString();
        }
    }
}

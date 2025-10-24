using Cortex.Helpers;
using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;
using GenerativeAI.Types;
using System.Text;
using System.Text.Json;
using static GeminiService.Api.Services.Implementations.GeminiService;
using Document = Cortex.Models.Document;

namespace Cortex.Services
{
    public class PreAnalysisStageService(IDocumentRepository documentRepository, IGeminiService geminiService,
        ILogger<PreAnalysisStageService> logger, IStageRepository stageRepository,
        IIndicatorService indicatorService, IAnalysisRepository analysisRepository,
        IIndexRepository indexRepository) : AStageService(documentRepository)
    {
        private readonly IGeminiService _geminiService = geminiService;
        private readonly IStageRepository _stageRepository = stageRepository;
        private readonly IIndexRepository _indexRepository = indexRepository;
        private readonly IIndicatorService _indicatorService = indicatorService;
        private readonly IAnalysisRepository _analysisRepository = analysisRepository;
        private readonly ILogger _logger = logger;


        #region Prompt Pre Analysis
        public string _promptPreAnalysis = """              
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

            Além disso, cada indicador pode ter uma lista de referências de onde ele foi extraído.
            Por exemplo: Se a análise tem a ver com identificar sentimentos (pode ser ou pode não ser), deve ser indicado
            qual trecho embasou a escolha do índice. 

            Exemplo da estrutura completa esperada:
            {{
              "indices": [
                {{
                  "name": "Threat Modeling",
                  "description": "Threat Modeling é o processo de identificar, comunicar e compreender ameaças e mitigações em um contexto de proteção de algo de valor.",
                  "indicator": "A presença da palavra Threat no texto.",
                  "references": [
                    {{
                      "document": "nome_arquivo",
                      "page": "2",
                      "line": "8"
                    }}
                  ]
                }},
                {{
                  "name": "Input Validation",
                  "description": "A validação de entrada previne que dados malformados entrem no sistema, sendo uma defesa crucial contra ataques de injeção.",
                  "indicator": "A não menção de validação de input durante uma entrevista.",
                  "references": [
                    {{
                      "document": "nome_arquivo",
                      "page": "2",
                      "line": "8"
                    }}
                  ]
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
        public override string GetPromptStageAsync()
        {
            return this._promptPreAnalysis;
        }

        public override async Task<AnalysisExecutionResult> ExecuteStageAsync(Analysis analysis)
        {
            _logger.LogInformation("Iniciando 'PreAnalysisStageService' para a Análise ID: {AnalysisId}...", analysis.Id);

            AnalysisExecutionResult resultBaseClass = await base.ExecuteStageAsync(analysis); // pega os documentos e embeddings         
            IEnumerable<Cortex.Models.Document> allDocuments = resultBaseClass.ReferenceDocuments.Concat(resultBaseClass.AnalysisDocuments);
            string finalPrompt = base.CreateFinalPrompt(resultBaseClass, analysis);

            var documentInfos = new List<DocumentInfo>();
            foreach (var doc in allDocuments)
            {
                // Usamos a propriedade GcsFilePath que você confirmou ter adicionado
                if (string.IsNullOrEmpty(doc.GcsFilePath) || !doc.GcsFilePath.StartsWith("gs://"))
                {
                    _logger.LogWarning("Documento ID {DocId} ('{Title}') está sem GcsFilePath. Pulando.", doc.Id, doc.Title);
                    continue;
                }

                documentInfos.Add(new DocumentInfo
                {
                    GcsUri = doc.GcsFilePath,
                    MimeType = doc.FileType.ToMimeType() // Assumindo que seu enum FileType tem este método helper
                });              
            }

            if (documentInfos.Count == 0)
            {
                _logger.LogError("Nenhum documento com GCS URI válido foi encontrado para a análise. Abortando.");
                resultBaseClass.ErrorMessage = "Nenhum documento válido foi encontrado para processamento.";
                resultBaseClass.IsSuccess = false;
                return resultBaseClass;
            }

            try
            {
                _logger.LogInformation("Enviando {Count} documentos e prompt para o Vertex AI (Gemini)...", documentInfos.Count);

                //peguei a ultima resposta e mockei pra n ficar gastando crédito
                string jsonResponse = """
                ```json
                {
                  "indices": [
                    {
                      "name": "Saber Pedagógico Lúdico",
                      "description": "Refere-se à utilização de jogos, brincadeiras, estórias e elementos teatrais como estratégia central para o ensino do Balé, especialmente para crianças. Essa abordagem, descrita como 'a gente aprende brincando' (p. 32), visa estimular a criatividade e a liberdade de expressão, transformando a aula em uma experiência prazerosa e significativa.",
                      "indicator": "Menção a brincadeiras (tubarão, coelho, castelo), 'teatrinho', uso de objetos cênicos (cenouras, comidinhas) e a percepção da aula como um espaço de diversão e criatividade.",
                      "references": [
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "32",
                          "line": "50"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "27",
                          "line": "20"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "33",
                          "line": "15"
                        }
                      ]
                    },
                    {
                      "name": "Saber Corporal Somático",
                      "description": "Conhecimento que aborda o Balé para além da forma estética, com foco na consciência corporal, anatomia e cinesiologia. A metodologia da professora é descrita como promotora de um 'conhecimento que protege o corpo' (p. 10), ensinando os alunos a se movimentarem de forma segura e a compreenderem a função de cada exercício, como na fala de Vagner: 'o que me faltava é o que o Balé dá: é a consciência corporal que o Balé dá, ã, a noção cinesiológica que o Balé dá, anatômica que o Balé dá...' (p. 7).",
                      "indicator": "Menção a explicações sobre anatomia, fisiologia e função dos movimentos, e ao balé como ferramenta de proteção corporal e prevenção de lesões.",
                      "references": [
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "10",
                          "line": "22"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "7",
                          "line": "26"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "4",
                          "line": "11"
                        }
                      ]
                    },
                    {
                      "name": "Saber Relacional Afetivo",
                      "description": "Refere-se à construção de um ambiente de aprendizagem baseado na afetividade, amizade, diálogo e respeito. A relação professor-aluno é descrita como 'de amizade!!! (risos) né? É o que era mais legal das aulas é que a gente se divertia' (p. 2), onde o erro é tratado com 'naturalidade' e 'de maneira bem humorada e divertida' (p. 9), e a aula se torna um espaço de encontro e bem-estar.",
                      "indicator": "Descrição da relação com a professora como sendo de amizade, carinho e proximidade; menção a um ambiente de aula divertido, acolhedor e onde o erro não é punido.",
                      "references": [
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "2",
                          "line": "20"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "9",
                          "line": "15"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "4",
                          "line": "1"
                        }
                      ]
                    },
                    {
                      "name": "Experiência da Formação Prática e Compartilhada",
                      "description": "Experiência vivenciada e reproduzida pela professora, originada em sua própria formação na escola de sua mestra, Dicléa. Caracteriza-se por um modelo de 'aprender fazendo', onde a docência é desenvolvida através da prática supervisionada ('eu fico do lado sempre durante um tempo acompanhando essa aula', p. 43) e da docência compartilhada, descrita por sua colega Eleonora como 'fundamental' (p. 14) para a troca de saberes.",
                      "indicator": "Relatos sobre o processo de se tornar professor através da prática, da observação, da supervisão direta e da colaboração com outros professores (docência compartilhada).",
                      "references": [
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "43",
                          "line": "17"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "14",
                          "line": "15"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "15",
                          "line": "20"
                        }
                      ]
                    },
                    {
                      "name": "Saber Pedagógico Reflexivo e Adaptativo",
                      "description": "Conhecimento pedagógico que se manifesta na capacidade de adaptar a metodologia aos diferentes contextos e alunos, respeitando o 'tempo de desenvolvimento de cada um' (p. 8). Inclui a prática de metarreflexão, incentivando os alunos a pensarem sobre seu próprio processo de aprendizagem e sobre como poderiam ensinar outros, como aponta Vagner: 'tu já ia explicando pro pessoal ali como é que dava pra adaptar aquilo pra outros contextos' (p. 7).",
                      "indicator": "Menção à adaptação dos exercícios para diferentes níveis de alunos, ao uso de diários para reflexão e a discussões sobre 'como ensinar'.",
                      "references": [
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "8",
                          "line": "25"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "7",
                          "line": "46"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "9",
                          "line": "38"
                        }
                      ]
                    },
                    {
                      "name": "Desconstrução da Visão Sacrificial do Balé",
                      "description": "Movimento de ressignificação do Balé, contrapondo-se à imagem tradicional de uma prática baseada no sacrifício e na dor. As aulas da professora são descritas como um espaço para 'desconstruir a ideia que eu tinha do Balé, principalmente! A ideia que eu tinha da coisa chata, da coisa sacrificante' (p. 1), mostrando que a técnica 'não precisa ser sacrificante, não precisa ser isso...' (p. 2).",
                      "indicator": "Oposição entre uma percepção anterior do balé (sofrimento, rigidez, 'coisa chata') e a experiência vivenciada nas aulas da professora (prazer, descoberta, leveza).",
                      "references": [
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "1",
                          "line": "40"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "2",
                          "line": "9"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "35",
                          "line": "23"
                        }
                      ]
                    },
                    {
                      "name": "Concepção de Formação Cultural Ampliada",
                      "description": "Entendimento da formação em dança como um processo que transcende a sala de aula e a técnica. A metodologia da professora promove uma formação integral, herdada de sua escola de origem, onde os alunos se envolvem em todo o universo da dança. Dicléa descreve: 'a escola não é só de dança, é corte costura, é cabelereiro, é tudo' (p. 46), e Eleonora reforça a preocupação em formar 'não só em termos de reprodução de passos, de vocabulário, mas de História da Dança' (p. 18).",
                      "indicator": "Menção a aprendizados teóricos (história, nomenclatura), à participação em todas as etapas da produção de um espetáculo (figurino, cenário) e a experiências formativas fora da sala de aula (viagens, festivais).",
                      "references": [
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "46",
                          "line": "26"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "18",
                          "line": "37"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "34",
                          "line": "38"
                        }
                      ]
                    }
                  ]
                }
                ```
                """;
                jsonResponse = Util.Util.SanitizeGeminiJsonResponse(jsonResponse);//sanitização da resposta
               
                //deixei comentado por enquanto pra não gastar recurso
                //string jsonResponse = await _geminiService.GenerateContentWithDocuments(documentInfos, finalPrompt);

                if (string.IsNullOrWhiteSpace(jsonResponse))
                {
                    _logger.LogError("O Vertex AI (GeminiService) retornou uma resposta vazia.");
                    resultBaseClass.ErrorMessage = "O serviço de IA retornou uma resposta vazia.";
                    resultBaseClass.IsSuccess = false;
                }
                else
                {
                    PreAnalysisStage stageEntity = new()
                    {
                        AnalysisId = analysis.Id
                    };

                    Stage newStageAdded = await _stageRepository.AddAsync(stageEntity); // SALVA O STAGE NO BD
                    _logger.LogInformation("Entidade 'PreAnalysisStage' (ID: {StageId}) salva.", stageEntity.Id);

                    JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
                    GeminiIndexResponse geminiResponse = JsonSerializer.Deserialize<GeminiIndexResponse>(jsonResponse, options); // erro aqui

                    if (geminiResponse == null || geminiResponse.Indices == null)
                    {
                        throw new JsonException("Falha ao desserializar a resposta do Gemini. Indices não foram encontrados.");
                    }

                    foreach (var geminiIndex in geminiResponse.Indices)
                    {
                        // 3a. Encontra ou Cria o 'Indicator'
                        Indicator indicatorEntity = await _indicatorService.GetOrCreateIndicatorAsync(geminiIndex.Indicator);

                        // 3b. Cria a entidade 'Index'
                        var newIndex = new Models.Index
                        {
                            Name = geminiIndex.Name,
                            Description = geminiIndex.Description,
                            IndicatorId = indicatorEntity.Id,
                            PreAnalysisStageId = newStageAdded.Id
                        };

                        // Mapeia as 'References' (do JSON para a Entidade)
                        if (geminiIndex.References != null)
                        {
                            foreach (var geminiRef in geminiIndex.References)
                            {
                                // Encontra o GCS URI correspondente ao nome do arquivo
                                string gcsUri = Util.Util.FindGcsUriFromFileName(allDocuments, geminiRef.Document);
                                if (gcsUri == null)
                                {
                                    _logger.LogWarning("Não foi possível encontrar o GCS URI para o documento de referência: {DocName}", geminiRef.Document);
                                    gcsUri = $"NÃO ENCONTRADO: {geminiRef.Document}";
                                }

                                IndexReference newReference = new()
                                {
                                    Index = newIndex, // EF Core associará automaticamente
                                    SourceDocumentUri = gcsUri,
                                    Page = geminiRef.Page,
                                    Line = geminiRef.Line,
                                    // aqui teria que pedir pra ele gerar jutno o trecho exato, além das páginas e linhas
                                    QuotedContent = $"Pág: {geminiRef.Page}, Linha: {geminiRef.Line}" 
                                };
                                newIndex.References.Add(newReference);
                            }
                        }
                        await _indexRepository.AddAsync(newIndex);
                    }
                    analysis.Stages.Add(newStageAdded);
                    resultBaseClass.PromptResult = jsonResponse;
                    resultBaseClass.IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha crítica ao chamar o GenerateContentWithDocuments do GeminiService.");
                resultBaseClass.ErrorMessage = $"Erro na API de IA: {ex.Message}";
                resultBaseClass.IsSuccess = false;
            }

            return resultBaseClass;
        }          
    }
}

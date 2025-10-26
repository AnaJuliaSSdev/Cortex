﻿using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;
using System.Text.Json;

namespace Cortex.Services
{
    public class PreAnalysisStageService(IDocumentRepository documentRepository,
        ILogger<PreAnalysisStageService> logger,
        IDocumentService documentService,
        IGeminiResponseHandler geminiResponseHandler, IPreAnalysisStageBuilder stageBuilder,
        IPreAnalysisPersistenceService preAnalysisPersistenceService, IGeminiService geminiService
        ) : AStageService(documentRepository)
    {
        private readonly IDocumentService _documentService = documentService;
        private readonly IGeminiResponseHandler _geminiResponseHandler = geminiResponseHandler;
        private readonly IPreAnalysisStageBuilder _stageBuilder = stageBuilder;
        private readonly IPreAnalysisPersistenceService _preAnalysisPersistenceService = preAnalysisPersistenceService;
        private readonly IGeminiService _geminiService = geminiService;
        private readonly ILogger _logger = logger;


        #region Prompt Pre Analysis
        public string _promptPreAnalysis = """              
            Você está na etapa de PRÉ ANÁLISE e irá fazer o LEVANTAMENTO DE ÍNDICES E A SELEÇÃO DOS INDICADORES dos documentos de análise, segundo a metodologia de Laurence Bardin.
            A partir dos documentos de análise (corpus), você deve:
                1. IDENTIFICAR índices: "Vestígios" ou sinais presentes na superfície do texto que sugerem a presença de fenômenos relacionados à pergunta central
                2. CONSTRUIR indicadores: Definir como medir/identificar sistematicamente a presença ou ausência desses índices
            
            CRITÉRIOS PARA SELEÇÃO DE ÍNDICES
            Os índices devem ser:
            - Pertinentes: Relacionados à pergunta central e às hipóteses
            - Observáveis: Identificáveis objetivamente no texto
            - Manifestos: Presentes explicitamente no discurso
            - Representativos: Relevantes para a inferência que se pretende fazer
            
            CRITÉRIOS PARA CONSTRUÇÃO DE INDICADORES
            Os indicadores devem ser:
            - Precisos: Definição clara e sem ambiguidade
            - Seguros: Permitem que outro analista chegue aos mesmos resultados
            - Sistemáticos: Aplicáveis de forma consistente em todo o corpus
            - Adequados ao objetivo: Quantitativos ou qualitativos conforme a necessidade

            Os documentos de contextualização devem ser utilizados como fontes de conceitos teóricos para embasar a análise.
            O foco central desta análise de conteúdo é responder a pergunta que move a análise, a pergunta central da pesquisa. Pode não ser necessariamente uma pergunta, mas uma tese, uma motivação de pesquisa. 
            Proceda com rigor metodológico. Sua análise deve ser replicável por outro pesquisador seguindo os mesmos critérios.          
            Você está na etapa de pré análise da metodologia e está extraindo os índices e indicadores dos documentos de análise, que são entrevistas transcritas. 
            Sua resposta DEVE ser obrigatoriamente um único bloco de código JSON, sem formatação Markdown, comentários ou texto introdutório. 
            Inclua informações precisas e referenciáveis. 
            O JSON deve ser um objeto contendo uma única chave "indices", que é uma lista de objetos.          
            Além disso, cada indicador pode ter uma lista de referências de onde ele foi extraído.
            Por exemplo: Se a análise tem a ver com identificar sentimentos (pode ser ou pode não ser), deve ser indicado
            qual trecho embasou a escolha do índice. 

            Retorne APENAS um objeto JSON válido, sem markdown, comentários ou texto adicional:
            O JSON DEVE SEGUIR EXATAMENTE ESSA ESTRUTURA:
            {{
              "indices": [
                {{
                  "name": "Nome do Índice",
                  "description": "Descrição clara do que é este índice e por que foi escolhido, com embasamento teórico",
                  "indicator": "Descrição PRECISA de como medir/identificar este índice",
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
            A ESTRUTURA ACIMA É A ESTRUTURA FINAL ESPERADA.

            Você irá receber todos os dados de contextualizãção e deverá preencher o json com os índices, cada índice deve conter um nome, e uma descrição. A descrição sobre o índice deve ser breve e conter um comentário sobre a escolha do índice e/ou o que ele representa na análise.

            PERGUNTA CENTRAL DA PESQUISA:
            {0}

            DOCUMENTOS DE CONTEXTUALIZAÇÃO ENVIADOS (nomes): 
            {1}

            DOCUMENTOS DE ANÁLISE ENVIADOS (nomes):
            {2}

            EXEMPLOS MAIS CONCRETOS DE ÍNDICES E INDICADORES (contexto fictício: Análise de entrevistas com professores sobre suas experiências durante a pandemia.):
            {{
              "indices": [
                {{
                  "name": "Dificuldades tecnológicas",
                  "description": "Menções a problemas com internet, plataformas, computador",
                  "indicator": "Presença de palavras: 'internet caiu', 'não funcionou', 'travou', 'não conseguia acessar'"
                }},
                {{
                  "name": "Cansaço e exaustão",
                  "description": "Expressões relacionadas a fadiga física ou mental",
                  "indicator": "Presença de palavras: 'cansado', 'exausto', 'esgotado', 'não aguento mais'"
                }},
                {{
                  "name": "Isolamento social",
                  "description": "Menções à falta de contato humano",
                  "indicator": "Presença de palavras: 'sozinho', 'falta de contato', 'distante', 'saudade dos alunos'"
                }},
                {{
                  "name": "Aprendizado de novas ferramentas",
                  "description": "Relatos sobre aprender a usar tecnologias",
                  "indicator": "Presença de expressões: 'aprendi a usar', 'descobri', 'dominei', 'me capacitei'"
                }},
                {{
                  "name": "Flexibilidade de horários",
                  "description": "Menções a benefícios relacionados ao tempo",
                  "indicator": "Presença de expressões: 'trabalhar em casa', 'meu horário', 'mais tempo', 'evitei trânsito'"
                }}
              ]
            }}

            REFERENCIAL TEÓRICO ADICIONAL SOBRE A COLETA DOS ÍNDICES E INDICADORES(trecho retirado do livro de Bardin):
            NOTA: ESSE CONTEÚDO É EXCLUSIVAMENTE PARA CONTEXTUALIZAÇÃO DA ETAPA QUE VOCÊ ESTÁ REALIZANDO
            
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
        public override string GetStagePromptTemplate()
        {
            return _promptPreAnalysis;
        }

        public override string FormatStagePromptAsync(Analysis analysis, AnalysisExecutionResult resultBaseClass, object? previousStageData = null)
        {
            // Pega os dados comuns da classe base
            string documentsNamesAnalysis = GetDocumentNames(resultBaseClass.AnalysisDocuments);
            string documentsNamesReferences = GetDocumentNames(resultBaseClass.ReferenceDocuments, "Nenhum documento de referência foi fornecido.");
            string centralQuestion = analysis.Question ?? "Nenhuma pergunta central foi definida.";

       
            // Pega o template desta etapa
            string template = GetStagePromptTemplate();

            // Formata com os 3 argumentos
            string formattedPrompt = string.Format(template,
                centralQuestion,           // {0}
                documentsNamesReferences,  // {1}
                documentsNamesAnalysis     // {2}
            );

            return formattedPrompt;
        }

        /// <summary>
        /// Serviço principal para execução da etapa de pré-análise.
        /// Orquestra todo o fluxo da pré análise: preparação, chamada à IA, processamento e persistência.
        /// </summary>
        public override async Task<AnalysisExecutionResult> ExecuteStageAsync(Analysis analysis)
        {
            _logger.LogInformation("Iniciando === 'PreAnalysisStageService' === para a Análise ID: {AnalysisId}...", analysis.Id);

            AnalysisExecutionResult resultBaseClass = await base.ExecuteStageAsync(analysis); // pega os documentos separados para montar o prompt       

            try
            {
                string finalPrompt = base.CreateFinalPrompt(analysis, resultBaseClass);
                IEnumerable<Cortex.Models.Document> allDocuments = resultBaseClass.ReferenceDocuments.Concat(resultBaseClass.AnalysisDocuments);
                List<DocumentInfo> documentInfos = _documentService.MapDocumentsToDocumentsInfo(allDocuments);

                _logger.LogInformation("Enviando {Count} documentos e prompt para o Vertex AI (Gemini)...", documentInfos.Count);
                //peguei a ultima resposta e mockei pra n ficar gastando crédito
                string jsonResponse = GetMockedGeminiResponse();
                //deixei comentado por enquanto pra não gastar recurso
                //string jsonResponse = await _geminiService.GenerateContentWithDocuments(documentInfos, finalPrompt);

                _logger.LogInformation("Resposta recebida do Gemini com sucesso.");

                GeminiIndexResponse geminiResponse = _geminiResponseHandler.ParseResponse<GeminiIndexResponse>(jsonResponse);

                _logger.LogInformation("Resposta processada: {Count} índices identificados.", geminiResponse.Indices.Count);

                PreAnalysisStage savedStage = await _preAnalysisPersistenceService.SavePreAnalysisAsync(analysis.Id);

                var indexes = await _stageBuilder.BuildIndexesAsync(
                geminiResponse,
                savedStage.Id,
                allDocuments
                 );

                await _preAnalysisPersistenceService.SaveIndexesAsync(indexes, savedStage.Id);

                resultBaseClass.PromptResult = jsonResponse;
                resultBaseClass.IsSuccess = true;
                resultBaseClass.PreAnalysisResult = savedStage;
                _logger.LogInformation("========== PRÉ-ANÁLISE CONCLUÍDA COM SUCESSO ==========");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Resposta inválida do serviço de IA.");
                resultBaseClass.ErrorMessage = ex.Message;
                resultBaseClass.IsSuccess = false;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Erro ao processar resposta JSON.");
                resultBaseClass.ErrorMessage = $"Erro ao processar resposta: {ex.Message}";
                resultBaseClass.IsSuccess = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha crítica ao executar pré análise.");
                resultBaseClass.ErrorMessage = $"Erro na etapa de pré análise: {ex.Message}";
                resultBaseClass.IsSuccess = false;
            }

            return resultBaseClass;
        }

        /// <summary>
        /// Retorna uma resposta mockada do Gemini para testes e desenvolvimento.
        /// TODO: Remover quando integração real com Gemini estiver ativa.
        /// </summary>
        /// <returns>JSON mockado da resposta do Gemini</returns>
        private string GetMockedGeminiResponse()
        {
            _logger.LogWarning("ATENÇÃO: Usando resposta MOCKADA do Gemini.");           
            return """
                ```json
                {
                  "indices": [
                    {
                      "name": "Saber Pedagógico e Didático",
                      "description": "Refere-se ao conhecimento sobre o processo de ensino-aprendizagem, incluindo didática, planejamento e a intencionalidade pedagógica. Este saber, advindo tanto da formação acadêmica quanto da prática supervisionada, diferencia a abordagem da professora de uma mera reprodução técnica, focando no 'porquê' e 'para quê' dos movimentos.",
                      "indicator": "Presença de termos e expressões como 'didática', 'metodologia', 'pedagogia', 'formação de professores', 'explicar pra que que é aquilo', 'preparar aula', 'organização da aula', 'relação professor-aluno', 'dar sentido'.",
                      "references": [
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "5",
                          "line": "10"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "4",
                          "line": "10"
                        }
                      ]
                    },
                    {
                      "name": "Experiências Formativas como Contraponto",
                      "description": "Menções a experiências passadas da professora ou de seus alunos, especialmente as negativas (como humilhação, dor, rigidez excessiva), que servem como base, por oposição, para a construção de sua própria metodologia de trabalho, buscando evitar a repetição de traumas e promover uma prática mais positiva.",
                      "indicator": "Relatos de experiências anteriores com o balé descritas com palavras como 'trauma', 'humilhante', 'sacrifício', 'sofrimento', 'rígido', 'maltrata o corpo', em contraste com a prática atual da professora.",
                      "references": [
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "4",
                          "line": "21"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "12",
                          "line": "1"
                        }
                      ]
                    },
                    {
                      "name": "Metodologia Centrada no Cuidado e Respeito ao Corpo",
                      "description": "Este índice aponta para uma abordagem pedagógica que prioriza o bem-estar do aluno, o respeito aos limites individuais, a prevenção de lesões e a construção de uma consciência corporal saudável, em oposição a uma visão do balé como prática de sacrifício.",
                      "indicator": "Presença de expressões como 'respeitar o tempo', 'limite do corpo', 'cuidado com o corpo', 'não precisa ser sacrificante', 'conhecimento que protege o corpo', 'consciência corporal'.",
                      "references": [
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "1",
                          "line": "30"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "10",
                          "line": "16"
                        }
                      ]
                    },
                    {
                      "name": "Desconstrução de Estereótipos do Balé",
                      "description": "Refere-se ao esforço consciente da professora em desafiar e transformar visões estereotipadas do balé como uma prática excessivamente rígida, dolorosa ou 'chata'. A metodologia busca ativamente apresentar uma 'outra visão' da dança, associando-a ao prazer e à leveza.",
                      "indicator": "Relatos que contrastam a experiência na aula da professora com ideias pré-concebidas de 'sacrifício', 'coisa chata', 'rígido', 'maltrata o corpo'. Uso de expressões como 'desconstruí a ideia que eu tinha', 'outra visão do Balé'.",
                      "references": [
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "1",
                          "line": "43"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "35",
                          "line": "18"
                        }
                      ]
                    },
                    {
                      "name": "Ambiente de Aprendizagem Afetivo e Lúdico",
                      "description": "Este índice caracteriza a criação de um clima de aula positivo, baseado em relações afetivas, bom humor, brincadeiras e elementos do imaginário. O erro é tratado como parte do processo e com leveza, constituindo uma estratégia pedagógica para engajar os alunos e facilitar a aprendizagem.",
                      "indicator": "Menções a 'amizade', 'se divertia', 'brincadeiras', 'teatrinho', 'lúdico', 'rir dos erros', 'era um divertimento', 'ambiente de intimidade'.",
                      "references": [
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "2",
                          "line": "18"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "32",
                          "line": "23"
                        }
                      ]
                    },
                    {
                      "name": "Corpo Próprio como Fonte de Saber",
                      "description": "Aponta para a importância da experiência corporal da própria professora como um saber fundamental em sua pedagogia. Suas vivências, dificuldades e superações no próprio corpo são transformadas em empatia e ferramentas para ensinar e facilitar a aprendizagem dos outros.",
                      "indicator": "Declarações que conectam a capacidade de ensinar da professora ao fato dela ter vivenciado as técnicas e dificuldades em seu próprio corpo. Expressões como 'sabe ensinar no corpo dos outros porque já passou por aquilo', 'as dificuldades te fizeram buscar isso'.",
                      "references": [
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "11",
                          "line": "22"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "12",
                          "line": "11"
                        }
                      ]
                    },
                    {
                      "name": "Estímulo à Prática Reflexiva e Criativa",
                      "description": "Refere-se ao uso de estratégias e ferramentas pedagógicas que incentivam os alunos a irem além da reprodução técnica, promovendo a reflexão sobre o próprio aprendizado, a autonomia e a expressão criativa.",
                      "indicator": "Menções a atividades como 'diários de processo', 'memorial no final da aula', 'liberdade de expressão', 'estimula a criatividade', 'criações', 'teatrinho', e momentos de tomada de decisão pelos alunos.",
                      "references": [
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "7",
                          "line": "28"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "33",
                          "line": "12"
                        }
                      ]
                    }
                  ]
                }
                ```
                """;
        }
    }
}

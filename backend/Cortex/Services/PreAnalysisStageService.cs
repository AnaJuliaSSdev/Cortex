using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;
using Google.Cloud.AIPlatform.V1;
using Type = Google.Cloud.AIPlatform.V1.Type;

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

        public  OpenApiSchema responseSchema = new OpenApiSchema
        {
            Type = Type.Object,
            Description = "Estrutura para extração de índices da análise de conteúdo",
            Properties =
            {
                { "indices", new OpenApiSchema
                    {
                        Type = Type.Array,
                        Description = "Lista de índices identificados nos documentos",
                        Items = new OpenApiSchema
                        {
                            Type = Type.Object,
                            Properties =
                            {
                                { "name", new OpenApiSchema
                                    {
                                        Type = Type.String,
                                        Description = "Nome do índice extraído"
                                    }
                                },
                                { "description", new OpenApiSchema
                                    {
                                        Type = Type.String,
                                        Description = "Descrição clara do que é este índice e por que foi escolhido, com embasamento teórico"
                                    }
                                },
                                { "indicator", new OpenApiSchema
                                    {
                                        Type = Type.String,
                                        Description = "Descrição PRECISA de como medir/identificar este índice"
                                    }
                                },
                                { "references", new OpenApiSchema
                                    {
                                        Type = Type.Array,
                                        Description = "Referências aos documentos onde o índice foi encontrado",
                                        Items = new OpenApiSchema
                                        {
                                            Type = Type.Object,
                                            Properties =
                                            {
                                                { "document", new OpenApiSchema
                                                    {
                                                        Type = Type.String,
                                                        Description = "Nome do arquivo fonte (ex: documento.pdf)"
                                                    }
                                                },
                                                { "page", new OpenApiSchema
                                                    {
                                                        Type = Type.String,
                                                        Description = "Número da página como string"
                                                    }
                                                },
                                                { "quoted_content", new OpenApiSchema
                                                    {
                                                        Type = Type.String,
                                                        Description = "O trecho exato do texto que justifica este índice"
                                                    }
                                                }
                                            },
                                            Required = { "document", "page", "quoted_content" }
                                        }
                                    }
                                }
                            },
                            Required = { "name", "description", "indicator", "references" }
                        }
                    }
                }
            },
            Required = { "indices" }
        };

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
            Além disso, cada indicador deve ter uma lista de referências de onde ele foi extraído.
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
                      "quoted_content": "O trecho exato do texto que justifica este índice"
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
                IEnumerable<Cortex.Models.Document> allDocumentsEnumerable = resultBaseClass.ReferenceDocuments.Concat(resultBaseClass.AnalysisDocuments);
                List<Cortex.Models.Document> allDocuments = [.. allDocumentsEnumerable];

                List<DocumentInfo> documentInfos = _documentService.MapDocumentsToDocumentsInfo(allDocuments);

                _logger.LogInformation("Enviando {Count} documentos e prompt para o Vertex AI (Gemini)...", documentInfos.Count);
                //peguei a ultima resposta e mockei pra n ficar gastando crédito
                string jsonResponse = GetMockedGeminiResponse();
                //deixei comentado por enquanto pra não gastar recurso
                //string jsonResponse = await _geminiService.GenerateContentWithDocuments(responseSchema, documentInfos, finalPrompt);

                _logger.LogInformation("Resposta recebida do Gemini com sucesso.");

                GeminiIndexResponse geminiResponse = _geminiResponseHandler.ParseResponse<GeminiIndexResponse>(jsonResponse);

                _logger.LogInformation("Resposta processada: {Count} índices identificados.", geminiResponse.Indices.Count);

                PreAnalysisStage savedStage = await _preAnalysisPersistenceService.SavePreAnalysisAsync(analysis.Id);

                var indexes = await _stageBuilder.BuildIndexesAsync(
                geminiResponse,
                savedStage,
                allDocuments
                 );

                await _preAnalysisPersistenceService.SaveIndexesAsync(indexes);
                resultBaseClass.AnalysisDocuments = resultBaseClass.AnalysisDocuments.ToList();
                resultBaseClass.ReferenceDocuments = resultBaseClass.ReferenceDocuments.ToList();
                resultBaseClass.IsSuccess = true;
                resultBaseClass.PreAnalysisResult = savedStage;
                resultBaseClass.AnalysisTitle = analysis.Title;
                resultBaseClass.AnalysisQuestion = analysis.Question;
                _logger.LogInformation("========== PRÉ-ANÁLISE CONCLUÍDA COM SUCESSO ==========");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Resposta inválida do serviço de IA.");
                resultBaseClass.ErrorMessage = ex.Message;
                resultBaseClass.IsSuccess = false;
            }
            catch (System.Text.Json.JsonException ex)
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
                      "name": "Apropriação de Elementos Estruturais do Texto (ENEM)",
                      "description": "Refere-se à percepção dos estudantes sobre o aprendizado e a aplicação de componentes específicos da redação modelo ENEM, como a proposta de intervenção e o uso de repertório sociocultural. A plataforma RevisãoOnline é citada como um meio para compreender e incorporar esses elementos, que são frequentemente novos ou pouco trabalhados no ensino formal anterior.",
                      "indicator": "Menção explícita ao aprendizado ou dificuldade com \"proposta de intervenção\", \"repertório\", \"citações\", \"estrutura do texto\", \"tese\", \"argumentos\" e como o RevisãoOnline auxiliou nesse processo.",
                      "references": [
                        {
                          "document": "i02 -.txt",
                          "page": "1",
                          "quoted_content": "Não, eu aprendi no RevisãoOnline. O que a Sheila passou em aula foi a estrutura básica, as teses, como elaborar elas e só. Então, tudo que eu sei de redação hoje em dia, eu sei por causa do revisão, ou foi assistindo vídeo aula que estão disponíveis ou pesquisando algo que eu não sabia direito."
                        },
                        {
                          "document": "i03 -.txt",
                          "page": "1",
                          "quoted_content": "a parte da proposta de intervenção que eu não sabia não fazia E o repertório legitimado também que eu não colocava não não sabia que precisava eu comecei a pensar comecei a estruturar foram as principais coisas a parte da proposta de intervenção que tem que marcar o quê como todas as coisinhas eu tenho uma memória meio visual assim e parece toda vez que eu escrevo a conclusão vem aquela foto das caixinhas da minha cabeça"
                        },
                        {
                          "document": "i16 -.txt",
                          "page": "1",
                          "quoted_content": "E ela é estruturada, tem que ser feito tal coisa, \"tipo: quem é que vai fazer isso?\". Aí tu tem que botar um agente e tal. \"O que que vai ter que fazer?\", \"Para qual objetivo?\"... Foi bem com o RevisãoOnline, eu não sabia antes."
                        },
                        {
                          "document": "i36 - Revisão Online - Matheus.pdf",
                          "page": "7",
                          "quoted_content": "Proposta de intervenção, eu acho que de certa forma é uma maneira interessante de te ajudar a concluir, por causa que as conclusões. Pelo que eu vi a revisão online ele tem cinco critérios pra proposta de intervenção, que é quem, o quê, como, o efeito e o detalhamento."
                        }
                      ]
                    },
                    {
                      "name": "Desenvolvimento da Consciência Metalinguística",
                      "description": "Este índice representa o momento em que os estudantes demonstram uma reflexão sobre a própria linguagem e a dos outros, identificando e nomeando fenômenos linguísticos. O RevisãoOnline, com seus critérios e marcações automáticas (como \"queísmo\"), age como um catalisador para essa tomada de consciência, levando-os a monitorar e aprimorar o uso de conectivos, repetições e a formalidade da escrita.",
                      "indicator": "Menção à descoberta ou ao ato de \"cuidar\", \"perceber\", \"prestar atenção\" em repetições de palavras, \"queísmo\", \"ondismo\", uso de conectivos, conjunções, e a formalidade do texto como resultado do uso da plataforma.",
                      "references": [
                        {
                          "document": "i01 - .pdf",
                          "page": "1",
                          "quoted_content": "Acho que aconteceu com palavras difíceis, as vezes eu olhava e dizia essa palavra é um erro, mas ele não apontava ai eu jogava no google e via que era uma palavra de verdade que existia, tinha significado."
                        },
                        {
                          "document": "i03 -.txt",
                          "page": "1",
                          "quoted_content": "o queismo, eu não sabia que existia primeiramente e aí revisando os textos quando chegava na parte do queísmo e ele mostrava todos os ques do texto ficava chocada de tanto que que o pessoal bota que eu boto também E aí isso é uma coisa que depois disso eu comecei a reparar e prestar atenção"
                        },
                        {
                          "document": "i12 -Entrevista Laura (120822).pdf",
                          "page": "7",
                          "quoted_content": "Ah! Voltando a parte do queísmo, por que é algo que eu percebi que eu utilizava bastante e as vez utilizo também, por que é algo que a gente fala no dia-a-dia, é isso que acontece nas redações, muitas vez ocorrem erros por causa do jeito que a gente fala no dia-a-dia, então foi algo que eu li e percebi: 'Caramba! Eu tenho que arrumar alguma forma de cuidar isso..."
                        },
                        {
                          "document": "i30 - Entrevista Guilherme.txt",
                          "page": "1",
                          "quoted_content": "bem legal a primeira vez que o que eu escutei sobre o queismo foi no próprio revisão online que daí só uns dois meses depois a gente viu isso em aula e isso ficou bastante na minha cabeça por causa que eu nunca tinha visto que até muita coisa que eu escrevi eu usava muito que e isso Ficou muito na minha cabeça de todos os tópicos o queismo ele ficou grudado o queismo e o ondismo"
                        }
                      ]
                    },
                    {
                      "name": "Aprendizagem por meio da Revisão por Pares",
                      "description": "Este índice capta a percepção dos estudantes de que o ato de revisar o texto de um colega é uma poderosa ferramenta de aprendizagem para a sua própria escrita. Ao analisar e aplicar os critérios do RevisãoOnline nos textos alheios, eles internalizam as regras, identificam erros comuns e desenvolvem um olhar mais crítico que é, subsequentemente, aplicado em suas próprias produções.",
                      "indicator": "Declarações explícitas de que \"revisar os outros\", \"corrigir outros textos\" ou \"ver os erros dos outros\" ajudou a melhorar a própria escrita, a não cometer os mesmos erros ou a entender melhor os critérios.",
                      "references": [
                        {
                          "document": "i06 -Camila.txt",
                          "page": "1",
                          "quoted_content": "tu analisar a escrita de outra pessoa entende os teus erros muito melhor então tua escrita já vem muito melhor Então eu acho que começar a incentivando a revisão é muito melhor"
                        },
                        {
                          "document": "i18-Junior.txt",
                          "page": "1",
                          "quoted_content": "a gente escrevia e revisava alguns textos né acho que a gente escreveu um e depois de duas ou três correções a gente corrigiu o nosso texto a gente corrigia outros também e depois que a gente corrigir esses outros era muito mais fácil escrever o nosso né"
                        },
                        {
                          "document": "i38 - Bianca.pdf",
                          "page": "3",
                          "quoted_content": "Me ajudou em muita coisa assim, perceber os erros de outras pessoas pra conseguir, como posso dizer? Pra não fazer o mesmo erro que os das outras pessoas."
                        },
                        {
                          "document": "i30 - Entrevista Guilherme.txt",
                          "page": "1",
                          "quoted_content": "Ah! Foi ótimo para o meu ego... de cara, logo, eu já vi, todas as redações eram redundância. Eu acho que todos os que eu avaliei sempre tinham a mesma coisa, redundância."
                        }
                      ]
                    },
                    {
                      "name": "Percepção sobre a Funcionalidade de Anonimato",
                      "description": "Refere-se à avaliação dos estudantes sobre o anonimato no processo de revisão por pares. O índice revela uma forte valorização dessa característica, vista como essencial para garantir a imparcialidade, reduzir o receio do julgamento, evitar constrangimentos entre colegas e, consequentemente, promover um ambiente mais seguro para a escrita e a revisão.",
                      "indicator": "Menções diretas à importância ou preferência pelo \"anonimato\", ou justificativas de que saber a identidade do autor/revisor poderia \"influenciar no julgamento\", \"deixar com vergonha\", ou criar \"preconceito\".",
                      "references": [
                        {
                          "document": "i03 -.txt",
                          "page": "1",
                          "quoted_content": "eu acho que anonimato é melhor porque se fosse se fosse pessoal ainda mais tu mais novo né Você tá no primeiro segundo ano não sei se pode ficar triste com colegas... tem várias várias probleminhas podem surgir se tiver o nome da pessoa Acho que fica melhor assim"
                        },
                        {
                          "document": "i06 -Camila.txt",
                          "page": "1",
                          "quoted_content": "entre colegas também é bem importante a ter imparcialidade eu acredito e no caso Anonimato né quanto o anonimato para trazer a imparcialidade."
                        },
                        {
                          "document": "i08 -GIovana.txt",
                          "page": "1",
                          "quoted_content": "Eu acho isso importante, pois de certa formar se tu souber de quem é a redação pode influenciar no teu julgamento quanto aquela análise ali."
                        },
                        {
                          "document": "i12 -Entrevista Laura (120822).pdf",
                          "page": "9",
                          "quoted_content": "Eu acho que até é melhor não saber quem avaliou e não saber quem você tá avaliando, por que pode... isso evita que tenham avaliações de má fé."
                        }
                      ]
                    },
                    {
                      "name": "Utilidade das Ferramentas de Revisão (Automática e Manual)",
                      "description": "Este índice aborda a percepção dos estudantes sobre a utilidade prática das diferentes ferramentas de marcação e comentário do RevisãoOnline. Inclui a avaliação sobre as sugestões automáticas, que servem como um \"norte\" inicial, e as ferramentas manuais (marcações inline, comentários), que permitem detalhar erros e sugerir melhorias, sendo cruciais para um feedback construtivo.",
                      "indicator": "Menções sobre como as \"marcações locais\", \"comentários\", \"sugestões automáticas\" ou a capacidade de \"marcar erros\" ajudaram a identificar problemas, a entender os erros, ou a dar um feedback mais preciso.",
                      "references": [
                        {
                          "document": "i09 - Entrevista 11_08_22(Isadora_captions).txt",
                          "page": "1",
                          "quoted_content": "É muito bom enxergar visualmente a repetição de palavras, que é um erro... que é o maior erro, a principal coisa que a gente mais comete, então é bom pra ti ter uma noção de quantas vezes tu repete aquela palavra."
                        },
                        {
                          "document": "i12 -Entrevista Laura (120822).pdf",
                          "page": "2",
                          "quoted_content": "Hã! Eu achei eles bem necessários, eu gostei bastante, porque permite que você mostre pra pessoa onde exatamente ela tem que melhorar."
                        },
                        {
                          "document": "i06 -Camila.txt",
                          "page": "1",
                          "quoted_content": "eu gosto dele porque ele te dá um Norte Sempre tu recebe um texto cru assim... mas daí tu aperta assim vem aquela marca aquelas marcações sugeridas daí tu tá agora entendi que onde eu teria começado é um Norte interessante"
                        },
                        {
                          "document": "i03 -.txt",
                          "page": "1",
                          "quoted_content": "eu notei que está bem diferente de como era ano passado para mim tem vários outros tipos de erro assim que tu pode colocar e eu achei que ficou bem mais completa agora que dá para especializar bem mais e eu gostei bastante porque ajuda a definir exatamente e quando a pessoa receber a redação ela consegue entender o que que foi mesmo que ela errou"
                        }
                      ]
                    },
                    {
                      "name": "Críticas e Sugestões de Usabilidade da Plataforma",
                      "description": "Este índice agrupa as percepções dos estudantes sobre as dificuldades, confusões e sugestões de melhoria relacionadas à interface e funcionamento do RevisãoOnline. São \"vestígios\" que indicam pontos de atrito na experiência do usuário, como funcionalidades escondidas, bugs, ou fluxos de navegação pouco intuitivos, que impactam indiretamente o processo de aprendizagem.",
                      "indicator": "Menção a problemas de usabilidade, como \"site trava\", \"botão de voltar apaga tudo\", \"comentário escondido\", \"confuso\", ou sugestões diretas de melhoria, como \"ter um mini tour\", \"melhorar a marcação\" ou \"mudar a disposição dos critérios\".",
                      "references": [
                        {
                          "document": "i02 -.txt",
                          "page": "1",
                          "quoted_content": "Creio que só mudaria o botão de comentário, pois ele é muito escondido, deixá-lo fixo na página."
                        },
                        {
                          "document": "i30 - Entrevista Guilherme.txt",
                          "page": "1",
                          "quoted_content": "Então já eu também eu queria muito falar sobre essa opção de voltar que eu acho que foi a coisa mais frustrante do revisãoonline para mim quando tu volta tu não ele apaga as informações que tu botou"
                        },
                        {
                          "document": "i36 - Revisão Online - Matheus.pdf",
                          "page": "10",
                          "quoted_content": "Talvez o que eu mais me confundi eu diria foi nos botões, mas eu acho que é porque eles eram novos, tipo era a primeira vez que eu tava usando essa versão, né? Então tinha ali o de esconder, o de editar, de marcar e eu tava tentando descobrir como eu deletava porque eu tinha feito uma correção meio errada e daí eu não tava achando"
                        },
                        {
                          "document": "i25 - Gabriela.txt",
                          "page": "1",
                          "quoted_content": "quando marcava assim por cima não dava para marcar por cima né eu marcava uma palavra daí depois eu queria marcar o parágrafo eu não consegui Daí tive uma dificuldade de marcar"
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

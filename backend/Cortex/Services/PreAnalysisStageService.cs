using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;
using Google.Cloud.AIPlatform.V1;
using Newtonsoft.Json;
using System.Text.Json;
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
                //string jsonResponse = GetMockedGeminiResponse();
                //deixei comentado por enquanto pra não gastar recurso
                string jsonResponse = await _geminiService.GenerateContentWithDocuments(responseSchema, documentInfos, finalPrompt);

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
                      "name": "Desconstrução da Visão Tradicional do Balé",
                      "description": "Este índice refere-se às menções que contrapõem a metodologia da professora a uma visão tradicional, rígida e sacrificial do Balé. A escolha se justifica por ser uma vivência que transformou a percepção das alunas sobre a dança, influenciando diretamente a prática pedagógica da professora, que se afastava do estereótipo do 'Balé que maltrata o corpo'.",
                      "indicator": "Presença de expressões que negam o caráter de sacrifício, dor ou rigidez excessiva do Balé, ou que relatam uma mudança de percepção de 'chato' e 'sofrido' para 'prazeroso' e 'agradável'. Busca por termos como 'desconstruí a ideia', 'não precisa ser sacrificante', 'coisa chata', 'maltrata o corpo, não!'.",
                      "references": [
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "1",
                          "quoted_content": "uma ideia muito fechada do Balé de ver, de sacrifício, e de... assim como eu posso dizer, uma...aquela coisa que Balé maltrata o corpo, não! E Eu não me sentia assim!!"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "2",
                          "quoted_content": "depois (risos) que eu fiz Balé com a Mônica eu vi que não precisa ser sacrificante, não precisa ser isso... porque eu não vou dançar Balé, não é pra isso, não é esse o objetivo, né, então não precisa ser assim dolorido"
                        }
                      ]
                    },
                    {
                      "name": "Afetividade na Relação Pedagógica",
                      "description": "Este índice aponta para a importância da afetividade, amizade e de uma relação mais horizontal entre professora e alunas como um elemento central da metodologia. A escolha é pertinente pois a qualidade do vínculo afetivo é citada como um saber que influencia diretamente o ambiente de aprendizagem e a adesão das alunas, sendo um pilar da prática pedagógica.",
                      "indicator": "Presença de termos e expressões que descrevem a relação com a professora e o ambiente da aula como 'afetividade', 'amizade', 'gostosa', 'se divertia', 'tranquilo', 'relação híbrida'.",
                      "references": [
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "1",
                          "quoted_content": "tirando a afetividade do grupo assim que tinha entre nós, eram aulas que contribuíam muito pra eu descobrir outras coisas possibilidades do meu corpo que eu não conhecia assim."
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "4",
                          "quoted_content": "De afetividade, ponto! Não tem outra (risos)... acho que deve ser isto em qualquer ambiente de aprendizagem, é o principal... assim, se não tiver isso, bah, se perde 50%."
                        }
                      ]
                    },
                    {
                      "name": "Consciência Corporal e Fundamentação Teórica",
                      "description": "Refere-se à prática da professora de explicar a funcionalidade anatômica e cinesiológica dos movimentos, promovendo uma consciência corporal para além da mera reprodução de formas. Este saber, vivenciado e transmitido, é um diferencial metodológico que, segundo os relatos, protege o corpo e dá sentido à prática.",
                      "indicator": "Identificação de relatos onde a professora explica o 'porquê' dos movimentos, utilizando noções de anatomia, fisiologia ou cinesiologia. Busca por termos como 'explicar pra que que é aquilo', 'consciência corporal', 'noção cinesiológica', 'anatômica', 'sentido'.",
                      "references": [
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "4",
                          "quoted_content": "Ah eu me lembro de abordagem teórica no sentido de explicar pra que que é aquilo...né, ãaa, por exemplo, porque que a gente tem que contrair o glúteo para ter equilíbrio e... sempre tu dizia [...] então sempre tinha esta explicação."
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "7",
                          "quoted_content": "o que me faltava é o que o Balé dá: é a consciência corporal que o Balé dá, ã, a noção cinesiológica que o Balé dá, anatômica que o Balé dá, fisiológica que o Balé dá, e tudo de proteção da articulação e de musculatura tudo"
                        }
                      ]
                    },
                    {
                      "name": "Ludicidade e Tratamento Positivo do Erro",
                      "description": "Este índice captura as menções ao uso de brincadeiras, jogos e a uma abordagem positiva do erro, tratando-o como parte do processo de aprendizagem e até como 'divertimento'. Esta experiência lúdica é um saber pedagógico que influencia a metodologia ao criar um ambiente leve, que diminui a ansiedade e estimula a participação.",
                      "indicator": "Presença de descrições de atividades lúdicas ou de uma atitude não-punitiva perante o erro. Busca por palavras como 'brincadeira', 'lúdico', 'divertimento', 'rir dos erros', 'teatrinho', 'não me sentia pressionada'.",
                      "references": [
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "2",
                          "quoted_content": "como eu ria muito dos meus erros e eu adoro os meus erros (risos)... e eu acho que era isso assim."
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "9",
                          "quoted_content": "o erro era tratado com naturalidade, porque ele faz parte né"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "32",
                          "quoted_content": "Aí no meio a gente fazia umas brincadeiras, umas coisas, tipo pular pelo tubarão do chão, que era pra treinar oooo... esqueci o nome do salto. Eeeee... a gente também fazia no final um teatrinho que a gente tinha que fazer passos de balé e essas coisas."
                        }
                      ]
                    },
                    {
                      "name": "Flexibilidade Metodológica e Adaptação ao Aluno",
                      "description": "Aponta para a capacidade da professora de adaptar sua metodologia às necessidades, tempos e capacidades individuais de cada aluno, respeitando seus limites. Esta experiência de ensino personalizado, que foge de uma abordagem única para todos, é um saber que constitui uma parte crucial de sua metodologia de trabalho.",
                      "indicator": "Identificação de trechos que descrevem o respeito ao tempo de cada aluno, a adaptação de exercícios ou a criação de uma metodologia 'universal' e aberta. Busca por expressões como 'respeitavas o tempo de desenvolvimento de cada um', 'trabalhar o Balé de forma universal', 'adaptar àquelas pessoas'.",
                      "references": [
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "8",
                          "quoted_content": "pra Vanessa tu dava um tipo de orientação, porque ela já tinha a uma outra capacidade, e pra mim tu ia dentro daquilo ali, pra eu compreender aquilo ali. Então ã, tu respeitavas o, o tempo de desenvolvimento de cada um."
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "3",
                          "quoted_content": "Então seria uma metodologia de trabalhar o Balé de forma universal assim, né. Enxergando qualquer possibilidade ali, ta aberta de repente por aluno também né?"
                        }
                      ]
                    },
                    {
                      "name": "Influência da Formação Acadêmica",
                      "description": "Este índice identifica as referências diretas à formação acadêmica da professora (Pedagogia, Educação Física) como uma influência em sua metodologia. Este saber teórico-prático é percebido pelas entrevistadas como a origem de sua 'didática', da capacidade de ensinar de forma diferente e de sua abordagem pedagógica mais ampla.",
                      "indicator": "Menção explícita ou implícita à formação acadêmica da professora como fonte de sua metodologia. Busca por termos como 'pedagoga', 'professora de Educação Física', 'formação acadêmica', 'didática', 'Educação Somática'.",
                      "references": [
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "13",
                          "quoted_content": "eu acho que, não sei se por tu ser pedagoga, assim, né, então por exemplo, 'vamos fazer' um [...] a gente brincava com aquilo, ia lá e era complicado, e era uma coisa meio de brincar um pouco"
                        },
                        {
                          "document": "EntrevistasExemplo.pdf",
                          "page": "24",
                          "quoted_content": "e também a tua formação pedagógica ajudava sabe, por que, tinha uma visão diferente, de ensinar dança, tu trazia muito de teu pedagógico, então eu acredito que essa relação com a teoria ela não era assim, agora é teoria, as coisas iam fluindo..."
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

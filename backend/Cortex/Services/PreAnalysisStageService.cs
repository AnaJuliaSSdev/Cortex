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

            ======== IMPORTANTE!============
            Para citar as referências dos índices, no trecho citado (quoted_content), utilize EXATAMENTE os textos dos documentos. NÃO ALTERE os textos dos trechos citados, 
            nem mesmo corrija eventuais erros de escrita. Essas citações devem ser preservadas pois deverão ser identificaveis no texto pelo usuário. 
            ================================

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
                      "name": "Aprendizagem pela Revisão por Pares",
                      "description": "Refere-se à percepção dos estudantes de que o ato de revisar os textos de seus colegas é uma forma eficaz de aprendizado. Ao analisar e aplicar os critérios de correção nos textos de outros, os alunos internalizam as regras, identificam erros comuns e, consequentemente, aprimoram a sua própria escrita. Este processo ativo de avaliação contribui para a qualificação da escrita.",
                      "indicator": "Presença de menções explícitas sobre aprender ao revisar o texto de outro colega, ver os erros dos outros para não cometer os mesmos, ou a percepção de melhora na própria escrita como consequência de ter revisado outros.",
                      "references": [
                        {
                          "document": "i06 -Camila.pdf",
                          "page": "6",
                          "quoted_content": "eu acho que eu sempre aprendi melhor daí as minhas redações consequentemente ficavam melhores porque eu já tinha revisado muito mais já tinha visto erros dos outros para cuidar para não cometer e ver se tu erros meus pensamentos eu escrevo assim mas não fica bom como eu achava que ficava ou é um erro até que eu achava que não era erro mas foi apontado pelo sugestão do revisor inline ali do texto e é um erro mesmo."
                        },
                        {
                          "document": "i38 - Bianca.pdf",
                          "page": "3",
                          "quoted_content": "Me ajudou em muita coisa assim, perceber os erros de outras pessoas pra conseguir, como posso dizer? Pra não fazer o mesmo erro que os das outras pessoas."
                        },
                        {
                          "document": "i02 -.pdf",
                          "page": "2",
                          "quoted_content": "houve uma evolução da minha parte por aprender como usar mais critérios para especificar melhor os erros das pessoas."
                        },
                        {
                          "document": "i12 -Entrevista Laura (120822).pdf",
                          "page": "7",
                          "quoted_content": "Eu aprendi bastante coisa, tanto que quando você tá revisando aparece ali os números pra você vê, pra você fazê uma melhor revisão, tem os pontos de interrogação então eu aprendi bastante sobre isso de fugir do tema ou não... Sobre os direitos humanos, eu sabia uma base assim, mas me incentivou a ir procurar mais a fundo pra fazê uma melhor avaliação..."
                        }
                      ]
                    },
                    {
                      "name": "Desenvolvimento da Autonomia e Estudo Autodirigido",
                      "description": "Este índice capta a forma como o RevisãoOnline funciona como uma fonte de conhecimento acessível a qualquer momento, permitindo que os estudantes busquem ativamente informações e aprendam de forma independente, para além das aulas formais. A plataforma fomenta a autonomia ao disponibilizar vídeos, dicas e outros recursos que os alunos utilizam para sanar dúvidas específicas.",
                      "indicator": "Menções ao uso de recursos da plataforma (vídeos, pesquisas) para aprender conceitos de forma autônoma, ou declarações de que o conhecimento foi adquirido 'por causa do revisão' e não apenas em aula.",
                      "references": [
                        {
                          "document": "i02 -.pdf",
                          "page": "1",
                          "quoted_content": "Não, eu aprendi no RevisãoOnline. O que a Sheila passou em aula foi a estrutura básica, as teses, como elaborar elas e só. Então, tudo que eu sei de redação hoje em dia, eu sei por causa do revisão, ou foi assistindo vídeo aula que estão disponíveis ou pesquisando algo que eu não sabia direito."
                        },
                        {
                          "document": "i08 -GIovana.pdf",
                          "page": "2",
                          "quoted_content": "Não, específico assim pra redação não, eu faço mais olhando vídeo no youtube e eras isso"
                        },
                        {
                          "document": "i18-Junior.pdf",
                          "page": "2",
                          "quoted_content": "o que eu usei naquela época foi alguma revisão assim material com a professora tinha disponibilizado ela tinha disponibilizado alguns vídeos para nós né para auxiliar na elaboração numa redação algum Google alguma coisa procurar o que que era o que que poderia ser o problema que foi apontado ali né para enquadrar eles né"
                        }
                      ]
                    },
                    {
                      "name": "Compreensão da Estrutura Textual",
                      "description": "O índice se refere à ajuda que a plataforma fornece para a compreensão e aplicação dos elementos estruturais da redação dissertativo-argumentativa, como a tese, o desenvolvimento dos argumentos e, especialmente, a proposta de intervenção, que é um componente específico e complexo do ENEM.",
                      "indicator": "Menções diretas à aprendizagem sobre como estruturar a redação, a tese, a conclusão ou a proposta de intervenção, e como a plataforma auxiliou nesse entendimento.",
                      "references": [
                        {
                          "document": "i03 -.pdf",
                          "page": "5",
                          "quoted_content": "a proposta de intervenção e revisando eu consegui começar a identificar e ver como cada partezinha da conclusão funciona isso é uma coisa que depois de usar revisão desse jeito eu comecei a ter esse olhar para as minhas redações e melhorou no meu quesito."
                        },
                        {
                          "document": "i08 -GIovana.pdf",
                          "page": "12",
                          "quoted_content": "E o Revisão émuito bom pois eu consigo analisar bem separadinho o primeiro e já sei o que eu tenho que procurar ali. e se eu não acho, eu consigo ver.. ah.. a tese não foi totalmente ou fugiu totalmente. Isso é muito bom no revisão."
                        },
                        {
                          "document": "i37 - Ana Laura.pdf",
                          "page": "7",
                          "quoted_content": "eu realmente definido assim que vai ter o quem o quê como . eu sempre sabia o que eu tinha que falar mais ou menos sobre isso mas não tão específico assim. eu acho eu eu gosto de da forma que está estruturado agora ali no revisão online porque de certa forma também foi como os professores do cursinho cobravam que eles cobravam que a gente tivesse na minha proposta de intervenção eu vou ter que ter essa esses itens então é um checklist até na hora de tu estou escrevendo eu preciso colocar isso"
                        },
                        {
                          "document": "i01 - .pdf",
                          "page": "1",
                          "quoted_content": "Normalmente que eu penso no primeiro parágrafo eu explico um pouquinho o tema e dou dois exemplos, por exemplo na redação de pequenas corrupções eu dei o exemplo de furar fila e sonegar imposto, no segundo e terceiro parágrafo eu desenvolvo esses dois exemplos que eu dei e no quarto eu digo como resolver eles."
                        }
                      ]
                    },
                    {
                      "name": "Conscientização sobre Erros Específicos",
                      "description": "Este índice aponta para a capacidade da ferramenta de destacar e tornar o aluno consciente de erros gramaticais e estilísticos específicos e recorrentes, como o 'queísmo', a repetição de palavras e o uso inadequado de conectivos, que muitas vezes passam despercebidos pelo próprio autor.",
                      "indicator": "Citações sobre a descoberta ou o aumento da percepção de erros como 'queísmo', repetição de palavras, ou uso de conjunções, atribuindo essa percepção ao uso da plataforma.",
                      "references": [
                        {
                          "document": "i03 -.pdf",
                          "page": "5",
                          "quoted_content": "o queismo, eu não sabia que existia primeiramente e aí revisando os textos quando chegava na parte do queísmo e ele mostrava todos os ques do texto ficava chocada de tanto que que o pessoal bota que eu boto também E aí isso é uma coisa que depois disso eu comecei a reparar e prestar atenção"
                        },
                        {
                          "document": "i08 -GIovana.pdf",
                          "page": "13",
                          "quoted_content": "Eu achei o revisão muito bom pois ele tem essa opção de repetição de palavras, então eu costumo analisar primeiro cada parágrafo e depois o conjunto. Então no primeiro parágrafo eu ja consigo ver se tem muita repetição das palavras. eu me policiava nisso."
                        },
                        {
                          "document": "i09 - Entrevista 11_08_22(Isadora_captions).pdf",
                          "page": "3",
                          "quoted_content": "É muito bom enxergar visualmente a repetição de palavras, que é um erro... que é o maior erro, a principal coisa que a gente mais comete, então é bom pra ti ter uma noção de quantas vezes tu repete aquela palavra."
                        },
                        {
                          "document": "i16 -.pdf",
                          "page": "5",
                          "quoted_content": "Sim!! Aquele estrangeirismo eu nem sabia que existia na vida, nem sabia que era para ficar tipo entre aspas ou em itálico e várias coisas tipo aquele \"porém\" né (tipo começar uma frase com \"mas\") não sabia também."
                        }
                      ]
                    },
                    {
                      "name": "Valorização do Anonimato para Feedback Honesto",
                      "description": "O anonimato no processo de revisão por pares é identificado como um fator crucial para a qualidade e honestidade do feedback. Os alunos sentem-se mais à vontade para apontar erros sem medo de retaliação social e para receber críticas de forma menos pessoal, o que resulta em uma avaliação percebida como mais 'verdadeira' e focada no texto.",
                      "indicator": "Menções à importância ou ao benefício do anonimato, seja para dar ou para receber a revisão, contrastando com a dificuldade de avaliar ou ser avaliado por conhecidos.",
                      "references": [
                        {
                          "document": "i16 -.pdf",
                          "page": "9",
                          "quoted_content": "Eu acho que é bom porque tipo assim num grupo da turma ali vai que a pessoa tem vergonha \"Nossa, não vou botar, avaliar certo que a pessoa foi tão ruim, então vou botar pelo menos alguma coisa que ela foi boa\". Eu acho que é um ponto bom porque daí a pessoa pode realmente marcar e não se preocupar \"Nossa, a pessoa vai me bater amanhã no recreio\", ela vai sem medo e ela marca e a outra já tem uma coisa mais \"verdadeira\"."
                        },
                        {
                          "document": "i08 -GIovana.pdf",
                          "page": "6",
                          "quoted_content": "Eu acho isso importante, pois de certa formar se tu souber de quem é a redação pode influenciar no teu julgamento quanto aquela análise ali. Por exemplo, tipo se eu pegar de uma amiga minha é claro que eu ia tentar entender tem mais compensa o jeito que ela quis trazer o termo, então como é anônimo eu acabo tipo deixando isso de lado eu vou analisar realmente o que está escrito"
                        },
                        {
                          "document": "i03 -.pdf",
                          "page": "8",
                          "quoted_content": "Acho que fica melhor assim e também tem a questão de que quando ninguém sabe que é tu se sente mais confortável de fazer as coisas então uma pessoa sente melhor de escrever sem ter medo de errar pode incentivar o aluno a fazer redação"
                        }
                      ]
                    },
                    {
                      "name": "Utilidade do Feedback Automatizado",
                      "description": "O feedback inicial, gerado automaticamente pelo algoritmo da plataforma, é percebido como um recurso útil. Ele serve como um ponto de partida para a revisão, destacando erros objetivos (ortografia, gramática) e dando um 'norte' para o revisor humano, que pode então focar em aspectos mais complexos da escrita.",
                      "indicator": "Declarações que avaliam positivamente a correção automática, mencionando sua assertividade, sua ajuda para encontrar erros de ortografia, ou sua função como um guia inicial para a revisão.",
                      "references": [
                        {
                          "document": "i02 -.pdf",
                          "page": "2",
                          "quoted_content": "Nunca atrapalhou, já teve vezes que trocou por sinônimos, geralmente ajuda pois acha vários erros ortográficos logo de cara, que ele acaba sendo mais assertivo."
                        },
                        {
                          "document": "i08 -GIovana.pdf",
                          "page": "8",
                          "quoted_content": "Achei importante, pois já mostra no primeiro momento o que tu vai ver, então pelo menos pra mim eu acabo focando em outras partes do texto porque se aquilo ali já foi mostrado, claro que depois eu vou dar uma olhada também mas eu vou focar em outras partes, esse já mostrou."
                        },
                        {
                          "document": "i06 -Camila.pdf",
                          "page": "3",
                          "quoted_content": "eu gosto dele porque ele te dá um Norte Sempre tu recebe um texto cru assim dá uma olhada tá"
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

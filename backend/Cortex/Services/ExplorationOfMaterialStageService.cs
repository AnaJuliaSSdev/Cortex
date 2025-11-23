using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;
using Google.Cloud.AIPlatform.V1;
using System.Text;
using Type = Google.Cloud.AIPlatform.V1.Type;

namespace Cortex.Services;

public class ExplorationOfMaterialStageService(IDocumentRepository documentRepository,
    IDocumentService documentService, IGeminiService geminiService, IGeminiResponseHandler geminiResponseHandler,
    IExplorationPersistenceService explorationPersistenceService,
    ILogger<ExplorationOfMaterialStageService> logger) : AStageService(documentRepository)
{
    private readonly ILogger _logger = logger;
    private readonly IDocumentService _documentService = documentService;
    private readonly IGeminiService _geminiService = geminiService;
    private readonly IGeminiResponseHandler _geminiResponseHandler = geminiResponseHandler;
    private readonly IExplorationPersistenceService _explorationPersistenceService = explorationPersistenceService;

    public OpenApiSchema responseSchema = new()
    {
        Type = Google.Cloud.AIPlatform.V1.Type.Object,
        Properties =
            {
                { "categories", new OpenApiSchema
                    {
                        Type = Type.Array,
                        Description = "Lista de categorias identificadas na análise",
                        Items = new OpenApiSchema
                        {
                            Type = Type.Object,
                            Properties =
                            {
                                { "name", new OpenApiSchema { Type = Type.String, Description = "Nome da categoria" } },
                                { "definition", new OpenApiSchema { Type = Type.String, Description = "Definição da categoria" } },
                                { "frequency", new OpenApiSchema { Type = Type.Integer, Description = "Número de unidades de registro nesta categoria" } },
                                {
                                    "register_units", new OpenApiSchema
                                    {
                                        Type = Type.Array,
                                        Description = "Unidades de registro desta categoria",
                                        Items = new OpenApiSchema
                                        {
                                            Type = Type.Object,
                                            Properties =
                                            {
                                                { "text", new OpenApiSchema { Type = Type.String, Description = "Trecho EXATO extraído do documento" } },
                                                { "document", new OpenApiSchema { Type = Type.String, Description = "Nome do arquivo fonte" } },
                                                { "page", new OpenApiSchema { Type = Type.String, Description = "Número da página" } },
                                                { "justification", new OpenApiSchema { Type = Type.String, Description = "Justificativa para categorização" } },
                                                { "found_indices", new OpenApiSchema
                                                    {
                                                        Type = Type.Array,
                                                        Description = "IDs dos índices encontrados nesta unidade",
                                                        Items = new OpenApiSchema { Type = Type.String, Description = "ID do índice" }
                                                    }
                                                },
                                                { "indicator", new OpenApiSchema { Type = Type.String, Description = "ID do indicador associado" } }
                                            },
                                            Required = { "text", "document", "page", "justification", "found_indices", "indicator" }
                                        }
                                    }
                                }
                            },
                            Required = { "name", "definition", "register_units" }
                        }
                    }
                }
            },
        Required = { "categories" }
    };

    public string _promptExplorationOfMaterial = """
    Você está na etapa de EXPLORAÇÃO DO MATERIAL e irá fazer o AGRUPAMENTO DAS UNIDADES DE REGISTRO EM **CATEGORIAS**.
    O foco central desta análise de conteúdo é responder a pergunta que move a análise, a pergunta central da pesquisa. Pode não ser necessariamente uma pergunta, mas uma tese, uma motivação de pesquisa. 
    Proceda com rigor metodológico. Sua análise deve ser replicável por outro pesquisador seguindo os mesmos critérios.
    Sua tarefa: A partir dos índices e indicadores fornecidos, você deve:
    1. IDENTIFICAR UNIDADES DE REGISTRO nos documentos de análise que contenham os índices definidos
    2. AGRUPAR essas unidades de registro em CATEGORIAS temáticas
    3. CONTAR a frequência de cada categoria (quantas unidades de registro pertencem a ela)

    CRITÉRIOS DE CONTAGEM (Unidade de Enumeração)
    Tipo: Frequência simples (descrito por Bardin)
    Unidade de registro: Segmento de texto (frase/trecho) que expressa uma ideia completa
    Referente: Apenas manifestações dos entrevistados
    Regra de contagem: Conte +1 para cada unidade de registro DISTINTA que:
      1.Contenha pelo menos um índice/indicador fornecido
      2.Expresse semanticamente o conteúdo da categoria
      3.Não seja repetição do mesmo tema no mesmo contexto
      4.Tenha sentido literal (desconsidere ironias ou negações)
    
    REGRAS RIGOROSAS
    1. Use APENAS os índices e indicadores fornecidos (referencie pelo nome sempre que for citá-los, nunca pelos ID's)
    2. Cada unidade de registro deve citar o trecho EXATO do documento fonte
    3. Categorias devem emergir do agrupamento temático das unidades
    4. Não invente ou infira informações não presentes nos textos
    5. Justifique cada classificação com base nos índices encontrados
    6. Mantenha fidelidade ao método de Bardin: categorias devem ser mutuamente exclusivas, homogêneas, pertinentes, objetivas e produtivas
    7. Caso você receba documentos em TXT, quando/se for referenciá-los, mantenha a página sempre como 1. 

    COMPREENSÃO IMPORTANTE:
    NÃO existe NECESSARIAMENTE uma relação 1:1 entre índices e categorias!
    - Índices são elementos concretos no texto (palavras, frases, expressões)
    - Unidades de registro são segmentos de texto que contêm um ou mais índices
    - Categorias são agrupamentos temáticos que emergem de múltiplas unidades de registro

    ======== AVISO IMPORTANTE!============
    Para citar as referências das unidades de registro, no trecho citado (text), utilize EXATAMENTE os textos dos documentos. NÃO ALTERE os textos dos trechos citados, 
    nem mesmo corrija eventuais erros de escrita. Essas citações devem ser preservadas pois deverão ser identificaveis no texto pelo usuário. 
    ================================
    
    Retorne APENAS um objeto JSON válido, sem markdown, comentários ou texto adicional:
    {{
      "categories": [
        {{
          "name": "Nome da Categoria",
          "definition": "Descrição clara do critério semântico que define esta categoria",
          "frequency": 0,
          "register_units": [
            {{
              "text": "Trecho EXATO extraído do documento",
              "document": "nome_arquivo",
              "page": "2", //sempre 1 para documentos TXT
              "found_indices": ["index_id_1", "index_id_2"],
              "indicator": "indicator_id_X",
              "justification": "Explicação de por que esta unidade pertence à categoria"
            }}
          ]
        }}
      ]
    }}

    Você irá receber todos os dados de contextualizãção e deverá preencher o json com as categorias.
    As definições da metodologia de Laurence Bardin devem ser seguidas a risca. NÃO EXISTE NECESSARIAMENTE UMA RELAÇÃO 1:1 ENTRE CATEGORIA E ÍNDICE. 
    ELES SÃO COISAS DIFERENTES METODOLOGICAMENTE.

    EXEMPLO DE CATEGORIAS BASEADAS EM ÍNDICES(contexto fictício: Análise de entrevistas com professores sobre suas experiências durante a pandemia):
    {{      
     EXEMPLO DE POSSÍVEIS ÍNDICES E INDICADORES DE UMA ANÁLISE:
      "indices": [
        {{
          "id": "idx_001",
          "name": "Dificuldades tecnológicas",
          "description": "Menções a problemas com internet, plataformas, computador",
          "indicator": "Presença de palavras: 'internet caiu', 'não funcionou', 'travou', 'não conseguia acessar'"
        }},
        {{
          "id": "idx_002",
          "name": "Cansaço e exaustão",
          "description": "Expressões relacionadas a fadiga física ou mental",
          "indicator": "Presença de palavras: 'cansado', 'exausto', 'esgotado', 'não aguento mais'"
        }},
        {{
          "id": "idx_003",
          "name": "Isolamento social",
          "description": "Menções à falta de contato humano",
          "indicator": "Presença de palavras: 'sozinho', 'falta de contato', 'distante', 'saudade dos alunos'"
        }},
        {{
          "id": "idx_004",
          "name": "Aprendizado de novas ferramentas",
          "description": "Relatos sobre aprender a usar tecnologias",
          "indicator": "Presença de expressões: 'aprendi a usar', 'descobri', 'dominei', 'me capacitei'"
        }},
        {{
          "id": "idx_005",
          "name": "Flexibilidade de horários",
          "description": "Menções a benefícios relacionados ao tempo",
          "indicator": "Presença de expressões: 'trabalhar em casa', 'meu horário', 'mais tempo', 'evitei trânsito'"
        }}
      ]
    }}

    POSSÍVEIS CATEGORIAS PARA OS ÍNDICES E INDICADORES LISTADOS ACIMA: 
    {{
      "categories": [
        {{
          "name": "Desafios do Ensino Remoto",
          "definition": "Agrupa dificuldades e obstáculos enfrentados durante o ensino online",
          "frequency": 15,
          "register_units": [
            {{
              "text": "A internet caiu três vezes durante a aula, foi horrível",
              "document": "entrevista_prof_01.pdf",
              "page": "2",
              "found_indices": ["idx_001"],
              "main_indicator": "idx_001", 
              "justification": "Relata problema técnico que prejudicou a aula"
            }},
            {{
              "text": "Me sentia completamente esgotado, não tinha energia para preparar aulas",
              "document": "entrevista_prof_01.pdf",
              "page": "3",
              "found_indices": ["idx_002"],
              "main_indicator": "idx_002",
              "justification": "Expressa exaustão relacionada ao trabalho remoto"
            }},
            {{
              "text": "O Zoom travou e perdi todos os alunos da sala",
              "document": "entrevista_prof_03.pdf",
              "page": "1",
              "found_indices": ["idx_001"],
              "main_indicator": "idx_001",
              "justification": "Outro problema técnico que impactou a aula"
            }},
            {{
              "text": "Sozinho na frente da tela, sem ver o rosto de ninguém, me sentia isolado",
              "document": "entrevista_prof_02.pdf",
              "page": "4",
              "found_indices": ["idx_003"],
              "main_indicator": "idx_003",
              "justification": "Relata impacto emocional do isolamento"
            }}
          ]
        }},
        {{
          "name": "Ganhos e Aprendizados",
          "definition": "Agrupa aspectos positivos e benefícios identificados na experiência",
          "frequency": 8,
          "register_units": [
            {{
              "text": "Aprendi a usar o Moodle, o Google Classroom, várias ferramentas novas",
              "document": "entrevista_prof_02.pdf",
              "page": "2",
              "found_indices": ["idx_004"],
              "main_indicator": "idx_004",
              "justification": "Relata aprendizado de competências tecnológicas"
            }},
            {{
              "text": "Trabalhando de casa, tive mais tempo com minha família",
              "document": "entrevista_prof_04.pdf",
              "page": "3",
              "found_indices": ["idx_005"],
              "main_indicator": "idx_005",
              "justification": "Identifica benefício relacionado à flexibilidade"
            }},
            {{
              "text": "Me capacitei em ferramentas digitais que nunca tinha usado",
              "document": "entrevista_prof_01.pdf",
              "page": "5",
              "found_indices": ["idx_004"],
              "main_indicator": "idx_004",
              "justification": "Outro relato de desenvolvimento de competências"
            }},
            {{
              "text": "Evitar duas horas de trânsito por dia foi libertador",
              "document": "entrevista_prof_03.pdf",
              "page": "2",
              "found_indices": ["idx_005"],
              "main_indicator": "idx_005",
              "justification": "Benefício relacionado ao tempo economizado"
            }}
          ]
        }}
      ]
    }}
    }}
    
    ÍNDICES E INDICADORES(RESULTANTES DA ETAPA DE PRÉ ANÁLISE):  
    {0}

    PERGUNTA CENTRAL DA PESQUISA:
    {1}

    DOCUMENTOS DE CONTEXTUALIZAÇÃO ENVIADOS (nomes): 
    {2}
    
    DOCUMENTOS DE ANÁLISE ENVIADOS (nomes):
    {3}

    REFERENCIAL TEÓRICO ADICIONAL SOBRE A ETAPA DE EXPLORAÇÃO DO MATERIAL E CODIFICAÇÃO(trechos retirados do livro de Laurence Bardin):
    NOTA: ESSE CONTEÚDO É EXCLUSIVAMENTE PARA CONTEXTUALIZAÇÃO DA ETAPA QUE VOCÊ ESTÁ REALIZANDO

    Se as diferentes operações da pré-análise forem convenientemente concluídas, a fase de análise propriamente dita não é mais do que a aplicação
    sistemática das decisões tomadas. Quer se trate de procedimentos aplicados
    manualmente ou de operações efetuadas por computador, o decorrer do
    programa completa-se mecanicamente. Esta fase, longa e fastidiosa, consiste
    essencialmente em operações de codificação, decomposição ou enumera-
    ção, em função de regras previamente formuladas.
    [...]
    Tratar o material é codificá-lo. A codificação corresponde a uma trans-
    formação - efetuada segundo regras precisas - dos dados brutos do texto,
    transformação esta que, por recorte, agregação e enumeração, permite atingir
    uma representação do conteúdo ou da sua expressão; suscetível de esclarecer
    o analista acerca das características do texto, que podem servir de índices, ou,
    como diz O. R. HolstP: A codificação é o processo pelo qual os dados brutos são transformados sis-
    tematicamente e agregados em unidades, as quais permitem uma descrição
    exata das características pertinentes do conteúdo.
    [...]
    A organização da codificação compreende três escolhas (no caso de uma
    análise quantitativa e categorial):
    • O recorte: escolha das unidades;
    • A enumeração: escolha das regras de contagem; (NOTA: ESSA ETAPA JÁ FOI REALIZADA E SERÁ FORNECIDA)
    • A classificação e a agregação: escolha das categorias. (NOTA: ESTAMOS NESSA ETAPA)
    [...]
    UNIDADES DE REGISTRO E DE CONTEXTO
    Quais os elementos do texto a ter em conta? Como recortar o texto em
    elementos completos? A escolha das unidades de registro e de contexto deve
    responder de maneira pertinente (pertinência em relação às características do
    material e face aos objetivos da análise).
    a) A unidade de registro - É a unidade de significação codificada e cor-
    responde ao segmento de conteúdo considerado unidade de base, visando a
    categorização e a contagem frequencial. A unidade de registro pode ser de
    natureza e de dimensões muito variáveis. Reina certa ambiguidade no que diz
    respeito aos critérios de distinção das unidades de registro. Efetivamente, exe-
    cutam-se certos recortes a nível semântico, por exemplo, o "tema', enquanto
    que outros são feitos a um nível aparentemente linguístico, como a "palavra"
    ou a "frase'",
    Isto serve de crítica a disciplinas cujo caráter científico e rigoroso é
    mais evidente. De fato, o critério de recorte na análise de conteúdo" é sem-
    pre de ordem semântica, ainda que, por vezes, exista uma correspondência
    com unidades formais (exemplos: palavra e palavra-tema; frase e unidade
    significante) ,
    A título ilustrativo podem ser citados entre as unidades de registro mais
    utilizadas:
    • A palavra: é certo que a "palavra" não tem definição precisa em lin-
    guística, mas para aqueles que fazem uso do idioma corresponde a qualquer
    coisa. Contudo, uma precisão linguística pode ser suscitada se for pertinente.
    Todas as palavras do texto podem ser levadas em consideração, ou pode-
    -se reter unicamente as palavras-chave ou as palavras-tema (symbpls em in-
    glês); pode igualmente fazer-se a distinção entre palavras plenas e palavras
    vazias; ou ainda efetuar-se a análise de uma categoria de palavras: substanti-
    vos, adjetivos, verbos, advérbios (...) a fim de se estabelecer quocientes
    [...]
        A unidade de contexto - A unidade de contexto serve de unidade de
    compreensão para codificar a unidade de registro e corresponde ao segmento
    da mensagem, cujas dimensões (superiores às da unidade de registro) são óti-
    mas para que se possa compreender a significação exata da unidade de registro.
    Esta pode, por exemplo, ser a frase para a palavra e o parágrafo para o tema.
    Com efeito, em muitos casos, torna-se necessário fazer (conscientemente)
    referência ao contexto próximo ou longínquo da unidade a ser registrada. Se
    vários codificadores trabalham num mesmo corpus, torna-se imprescindível
    um acordo prévio. Por exemplo, no caso de análise de mensagens políticas,
    palavras como liberdade, ordem, progresso, democracia, sociedade, têm ne-
    cessidade de contexto para serem compreendidas no seu verdadeiro sentido.
    A referência ao contexto é muito importante para a análise avaliativa e para
    a análise de contingência. Os resultados são suscetíveis de variar sensivel-
    mente segundo as dimensões de uma unidade de contexto. A intensidade e a
    extensão de uma unidade podem surgir de modo mais ou menos acentuado,
    consoante as dimensões da unidade de contexto escolhida. No que se refere
    às co ocorrências, é evidente que o seu número aumenta com as dimensões
    da unidade de contexto: é pouco provável, por exemplo, que se possam en-
    contrar temas semelhantes num parágrafo: ou em alguns minutos de grava-
    ção, mas a probabilidade aumenta num texto de várias páginas ou numa
    emissão de uma hora. Geralmente, quanto maior é a unidade de contexto
    mais as atitudes ou valores se afirmam numa análise avaliativa, ou mais nu-
    merosas são as co ocorrências numa análise de contingência.
    A determinação das dimensões da unidade de contexto é presidida por
    dois critérios: o custo e a pertinência. É evidente que uma unidade de contex-
    to alargado exige uma releitura do meio, mais vasta. Por outro lado, existe
    uma dimensão ótima, ao nível do sentido: se a unidade de contexto for muito
    pequena ou muito grande, já não se encontra adaptada; também aqui são de-
    terminantes quer o tipo de material, quer o quadro teórico.
    De qualquer modo, é possível testar as unidades de registro e de contexto
    em pequenas amostras, a fim de que nos asseguremos que operamos com os
    instrumentos mais adequados
    [...]
        l38 ANÁLISE DE CONTEÚDO
    2. REGRAS DE ENUMERAÇÃO
    É necessário fazer a distinção entre a unidade de registro - o que se
    conta - e a regra de enumeração - o modo de contagem.
    Vejamos o seguinte exemplo: temos um "texto" concluído, em que a
    identificação e o recorte forneceram os elementos ou unidades de registro
    (palavras, temas ou outras unidades) seguintes:
    a, d, a, e, a, b.
    Sabendo-se que a lista de referência, estabelecida a partir de um conjun-
    to de "textos': ou, segundo uma norma, é a, b, c, d, e, fi é possível utilizar diver-
    sos tipos de enumerações:
    A frequência(NOTA: MEDIDA QUE DEVE SER USADA NO NOSSO CASO): geralmente é a medida mais usada. Corresponde ao se-
    guinte postulado (válido em certos casos e em outros não): a importância de
    uma unidade de registro aumenta com a frequência de aparição. No nosso
    exemplo, a frequência de cada elemento é:
    a = 3;
    b = 1;
    c = O;
    d = 1;
    e = 1;
    f = O.
    Uma medida frequencial em que todas as aparições possuam o mes-
    mo peso postula que todos os elementos tenham uma importância igual.
    A escolha da medida frequencial simples não deve ser automática. É preciso
    lembrarmo-nos de que ela assenta no seguinte pressuposto implícito: a
    A CODIFICAÇÃO l39
    aparição de um item de sentido ou de expressão será tanto mais significa-
    tiva - em relação ao que procura atingir na descrição ou na interpretação
    da realidade visada - quanto mais esta frequência se repetir. A regularida-
    de quantitativa de aparição é, portanto, aquilo que se considera como sig-
    nificativo. Isto supõe que todos os itens tenham o mesmo valor, o que nem
    sempre acontece .
    [...]
    A categorização é uma operação de classificação de elementos constituti-
    vos de um conjunto por diferenciação e, em seguida, por reagrupamento segun-
    do o gênero (analogia), com os critérios previamente definidos. As categorias
    são rubricas ou classes, as quais reúnem um grupo de elementos (unidades de
    registro, no caso da análise de conteúdo) sob um título genérico, agrupamen-
    to esse efetuado em razão das características comuns destes elementos. O cri-
    tério de categorização pode ser semântico (categorias temáticas: por exemplo,
    todos os temas que significam a ansiedade ficam agrupados na categoria "an-
    siedade'" enquanto que os que significam a descontração ficam agrupados sob
    o título conceitual "descontração"), sintático (os verbos, os adjetivos), léxico
    (classificação das palavras segundo o seu sentido, com emparelhamento dos
    sinônimos e dos sentidos próximos) e expressivo (por exemplo, categorias que
    classificam as diversas perturbações da linguagem).
    [...]
        ANÁLISE DE CONTEÚDO148
    A atividade taxonômica é uma operação muito vulgarizada de repartição
    dos objetos em categorias. Se antes de colocarmos um disco no toca-discos
    nos interrogarmos sobre a vontade que temos de ouvir Bach, Ravel ou Boulez,
    não utilizamos o mesmo critério que preside às escolhas possíveis, caso nos
    interroguemos acerca do desejo de ouvir violino, órgão ou piano. O critério de
    categorização não é o mesmo (compositor ou instrumento). Não acentuamos
    o mesmo aspecto da realidade. Por outro lado, o critério que empregamos é
    mais ou menos adaptado à realidade que se nos oferece. É possível que os nos-
    sos dois desejos convirjam e venham precisar a escolha por nós feita (um de-
    terminado instrumento e um determinado compositor). De igual modo, em
    análise de conteúdo, a mensagem pode ser submetida a uma ou várias dimen-
    sões de análise.
    Classificar elementos em categorias impõe a investigação do que cada
    um deles tem em comum com outros. O que vai permitir o seu agrupamento
    é a parte comum existente entre eles. É possível, contudo, que outros critérios
    insistam em outros aspectos de analogia, talvez modificando consideravel-
    mente a repartição anterior.
    A categorização é um processo de tipo estruturalista e comporta duas
    etapas:
    • o inventário: isolar os elementos;
    • a classificação: repartir os elementos e, portanto, procurar ou impôr
    certa organização às mensagens.
    [...]
        XEMPLOS DE CONJUNTOS CATEGORIAIS
    Se na maioria dos casos se torna necessário criar uma grade de catego-
    rias para cada nova análise, os estudos anteriores são suscetíveis de inspirar o
    analista. É por este motivo que vamos citar alguns exemplos de conjuntos ca-
    tegoriais já utilizados.
    A CATEGORIZAÇÃO 151
    a) A análise dos valores
    Exemplo 1:
    White especializou-se, logo após a Segunda Guerra Mundial, na análise
    de valores. Analisa, em primeiro lugar, a autobiografia de Richard Wright,
    Black Boy (1947); em seguida, analisa o estilo de propaganda de Hitler e Roo-
    sevelt (1949) e, mais tarde, os discursos de Kennedy e de Kruschev (1967).
    Propomos uma das suas grades de análise'.
    A / Valores fisiológicos
    1. Alimentação
    2. Sexo
    3. Repouso
    4. Saúde
    5. Segurança
    6. Conforto
    D/Valores que exprimem o medo
    (segurança emocional)
    E / Valores de jogo e de alegria
    1. Experiência nova
    2. Excitação, emoção
    3. Beleza
    4. Humor
    5. Autoexpressão criativa
    F / Valores práticos
    1. Sentido prático
    2. Possessão
    3. Trabalho
    G / Valores cognitivos
    1. Conhecimento
    H/ Diversos
    1. Felicidade
    2. Valor em geral
    B / Valores sociais
    1. Amor sexual
    2. Amor familiar
    3. Amizade
    C / Valores relativos ao ego
    1. Independência
    2. Cumprimento
    3. Reconhecimento
    4. Amor-próprio
    5. Dominação
    6. Agressão
    Exemplo 2:
    V. Isambert- Iamati" mostrou a evolução dos valores pregados pela ins-
    tituição escolar entre 1860 e 1965, a partir da análise de uma amostra de
    discursos de distribuição de prêmios; proferidos por vários oradores direta
    ou indiretamente envolvidos no ensino médio, produzidos regularmente du-
    2. R. K. White, Value-analisis: the nature and use of the method, Glen Gardiner, N. J., Liberta-
    rian Press, 1951. Citado por Holsti, op. cit.
    3. Isambert- [amati, Crises de la societé, crises de l'enseignement, P. U. E, 1970.
    152 ANÁLISE DE CONTEÚDO
    rante esse período e de fácil acesso, esses discursos de entrega de prêmios ser-
    viram de material de base para todo um estudo sobre a "moral de referência"
    da escola, acerca dos fins - e dos meios para se atingirem esses fins - visados
    pela instituição escolar e ainda sobre os objetos de conhecimento intelectual a
    promover etc.
    Um conjunto de cinco categorias e subcategorias serviu de base à análise.
    • As mudanças que o ensino das disciplinas escolares deve produzir nos
    alunos:
    • Participação nos valores supremos.
    • Aperfeiçoamento individual procurado pelo próprio aluno.
    • Exercício de mecanismos operatórios.
    • Os objetos a conhecer:
    • Os homens do passado e as suas obras.
    • Os homens contemporâneos.
    • A natureza humana e universal.
    • A natureza.
    • Os objetos da educação moral:
    • Lealdade em relação à Universidade nacional e laica,
    • Lealdade em relação ao estabelecimento.
    • Exílio do mundo como condição vantajosa para a educação,
    • Valor educativo da disciplina.
    • Ação dos pares na formação do caráter.
    • Consideração das diferenças individuais entre os alunos.
    • Utilização das tendências lúdicas.
    • Exemplo moral dos professores,
    • Ascendente voluntário dos professores.
    • A definição institucional:
    • É bom que a definição central do ensino médio mude, para que se
    adapte às mudanças socias.
    • A escolaridade de nível médio deve ser longa.
    • O ensino médio deve bastar aos alunos, sem que lhes seja necessá-
    rio continuar os estudos.
    • Os liceus não devem servir para preparar o futuro profissional dos
    alunos.
    • O público visado é a elite social.
    A CATEGORIZAÇÃO
    • Os valores de referência:
    • Moral individual de perfeição ou de imperativo categórico.
    • Moral individual de tendência hedonista, ou de tipo "higiene mental':
    • Moral individual de solidariedade.
    • Exortação ao trabalho.
    • Exaltação do progresso.
    • Exaltação da juventude.
    • Exaltação da família.
    • Exaltação da pátria.
    • Exaltação da paz e da compreensão internacional.
    A conclusão final deste estudo demonstra que as mudanças da sociedade
    francesa se repercutem nos objetivos que os sistemas de ensino propõem e
    que as crises da sociedade e as do ensino aparecem sincronizadas. Os objeti-
    vos da instituição escolar evoluem. Desse modo, é possivel dividir os períodos
    conforme os valores dominantes:
    1) 1860-1870: valores supremos e integração na elite.
    2) 1876-1885: integração na elite e transformação do mundo.
    3) 1896-1905: transformação do mundo e entusiasmo laico.
    4) 1906-1930: gratuidade da cultura.
    5) 1931-1940: aprender a aprender.
    6) 1946-1960: o ensino médio se defende: retorno ao estetismo.
    7) 1961-1965: crises dos objetivos".
    Sob o ponto de vista técnico, as análises foram essencialmente temáticas,
    mas sempre afinadas por precauções como a ponderação dos temas, a divisão em
    temas principais e secundários, a abordagem avaliativa (texto favorável, texto neu-
    tro) e a utilização de relações de gênero "coeficiente de dominância':
    b) A análise dos fins e dos meios
    Exemplo 1:
    Trata-se de uma análise dos objetivos afetivos e objetivos racionais efetu-
    ada por B. Berelson e P. Salter, acerca das revistas populares de ficção". Foram
    utilizados dois sistemas de categorias:
    4. E a seguir vem o maio de 68!
    5 B. Berelson e P. Salter, "Majority and minority Americans: an analysis of magazine fictíon',
    Publ. Opino Quart., 1946.
    153
    154 ANÁLISE DE CONTEÚDO
    A / Intenções do "coração"
    1. Solução de problemas concretos
    2. Progresso pessoal
    3. Dinheiro e bens materiais
    4. Afeição e segurança emocional
    5. Segurança econômica e social
    6. Poder e dominação 
    7. Patriotismo
    8. Aventura
    9. Justiça
    10. Independência
    Exemplo 2:
    Este estudo analisa as finalidades e possibilidades de êxito oferecidas
    às crianças nos programas televisivos, relacionando-as com os meios pre-
    conizados",
    A / Categorias das finalidades
    1. Propriedade (êxito material)
    2. Autopreservação (desejo de statu quo, inclusive)
    3. Afeição
    4. Sentimento
    5, Poder e prestígio
    6. Objetivos psicológicos (inclusive violência e educação)
    7. Outros
    B / Categorias dos métodos
    1. Legais
    2. Não legais (sem feridas nem estragos)
    3. Econômicas
    4. Violência
    5. Organização, negociação e compromisso
    6. Evasão, fuga (tentativa de evitar os fatos inerentes à realização do
    objetivo, esquecimento da finalidade etc.)
    7. Acaso
    8. Outras
    6. O. N. Larson, L. N. Gray e J. G. Fortis, "Goals and goal-achievement methods in television
    content: model for anomie?" em Social Inquiry, 33, 1963.   
    """;

    public override string GetStagePromptTemplate()
    {
        return _promptExplorationOfMaterial;
    }

    public override string FormatStagePromptAsync(Analysis analysis, AnalysisExecutionResult resultBaseClass, object? previousStageData = null)
    {
        // Pega os dados comuns da classe base
        string documentsNamesAnalysis = GetDocumentNames(resultBaseClass.AnalysisDocuments);
        string documentsNamesReferences = GetDocumentNames(resultBaseClass.ReferenceDocuments, "Nenhum documento de referência foi fornecido.");
        string centralQuestion = analysis.Question ?? "Nenhuma pergunta central foi definida.";

        // Pega os dados da etapa anterior (Índices)
        // Precisamos buscar a etapa anterior do banco ou recebê-la
        var preAnalysisStage = analysis.Stages
                                    .OfType<PreAnalysisStage>() // Filtra para o tipo correto
                                    .FirstOrDefault();

        string formattedIndices = "Nenhum índice encontrado na etapa anterior.";
        if (preAnalysisStage != null && preAnalysisStage.Indexes != null && preAnalysisStage.Indexes.Any())
        {
            // Formata os índices como uma string legível para o LLM
            formattedIndices = FormatIndicesForPrompt(preAnalysisStage.Indexes);
        }
        else
        {
            _logger.LogWarning("Não foi possível encontrar resultados da PreAnalysisStage para formatar o prompt da ExplorationOfMaterialStage.");
            // Você pode querer lançar uma exceção aqui se a etapa anterior for obrigatória
        }


        // Pega o template desta etapa
        string template = GetStagePromptTemplate();

        // Formata com os 4 argumentos
        string formattedPrompt = string.Format(template,
            formattedIndices,          // {0}
            centralQuestion,           // {1}
            documentsNamesReferences,  // {2}
            documentsNamesAnalysis     // {3}
        );

        return formattedPrompt; // Não precisa de Task.FromResult se a lógica for síncrona aqui
    }

    public override async Task<AnalysisExecutionResult> ExecuteStageAsync(Analysis analysis)
    {
        _logger.LogInformation("Iniciando === 'ExplorationOfMaterialStageService' === para a Análise ID: {AnalysisId}...", analysis.Id);
        AnalysisExecutionResult resultBaseClass = await base.ExecuteStageAsync(analysis); // pega os documentos separados para montar o prompt

        try
        {
            string finalPrompt = base.CreateFinalPrompt(analysis, resultBaseClass, null);
            IEnumerable<Cortex.Models.Document> allDocuments = resultBaseClass.ReferenceDocuments.Concat(resultBaseClass.AnalysisDocuments);
            List<DocumentInfo> documentInfos = _documentService.MapDocumentsToDocumentsInfo(allDocuments);

            _logger.LogInformation("Enviando {Count} documentos e prompt para o Vertex AI (Gemini)...", documentInfos.Count);
            //peguei a ultima resposta e mockei pra n ficar gastando crédito
            //string jsonResponse = GetMockedGeminiResponse();
            //deixei comentado por enquanto pra não gastar recurso
            string jsonResponse = await _geminiService.GenerateContentWithDocuments(responseSchema,documentInfos, finalPrompt);

            _logger.LogInformation("Resposta recebida do Gemini com sucesso.");

            resultBaseClass.PromptResult = jsonResponse;

            GeminiCategoryResponse geminiResponse = _geminiResponseHandler.ParseResponse<GeminiCategoryResponse>(jsonResponse);
          
            ExplorationOfMaterialStage stageEntity = await _explorationPersistenceService.MapAndSaveExplorationResultAsync(analysis.Id, geminiResponse, allDocuments);

            resultBaseClass.AnalysisQuestion = analysis.Question;
            resultBaseClass.AnalysisTitle = analysis.Title;
            resultBaseClass.ExplorationOfMaterialStage = stageEntity;
            resultBaseClass.PromptResult = jsonResponse;
            resultBaseClass.IsSuccess = true;
            _logger.LogInformation("========== EXPLORAÇÃO DO MATERIAL CONCLUÍDA COM SUCESSO ==========");

            return resultBaseClass;
        }
        catch (System.Text.Json.JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Falha ao desserializar a resposta JSON da Exploração de Material.");
            resultBaseClass.ErrorMessage = "Erro ao processar a resposta da IA (formato inválido).";
            resultBaseClass.IsSuccess = false;
            // Retorne ou trate o erro
            return resultBaseClass;
        }
        catch (ArgumentNullException argNullEx)
        {
            _logger.LogError(argNullEx, "A resposta JSON recebida estava vazia.");
            resultBaseClass.ErrorMessage = "O serviço de IA retornou uma resposta vazia.";
            resultBaseClass.IsSuccess = false;
            return resultBaseClass;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha genérica na exploração de material.");
            resultBaseClass.ErrorMessage = ex.Message;
            resultBaseClass.IsSuccess = false;
            return resultBaseClass;
        }
    }

    private static string FormatIndicesForPrompt(ICollection<Models.Index> indices)
    {
        // Cria uma representação em string dos índices para incluir no prompt
        StringBuilder sb = new();
        foreach (var index in indices)
        {
            sb.AppendLine($"- Índice: {index.Name} (ID: {index.Id})");
            sb.AppendLine($"  Descrição: {index.Description}");
            if (index.Indicator != null)
            {
                sb.AppendLine($"  Indicador: {index.Indicator.Name} (ID: {index.IndicatorId})");
            }
        }
        return sb.ToString();
    }

    private string GetMockedGeminiResponse()
    {
        _logger.LogWarning("ATENÇÃO: Usando resposta MOCKADA do Gemini.");
        return """
            ```json
            {
              "categories": [
                {
                  "name": "Apropriação da Estrutura e Normas do Gênero Dissertativo-Argumentativo (ENEM)",
                  "definition": "Esta categoria agrupa unidades onde os estudantes relatam o aprendizado e a aplicação dos componentes estruturais e convenções específicas da redação modelo ENEM, como a proposta de intervenção, o uso de repertório sociocultural e a construção da tese, identificando o RevisãoOnline como um recurso fundamental para essa apropriação.",
                  "register_units": [
                    {
                      "text": "Então, tudo que eu sei de redação hoje em dia, eu sei por causa do revisão, ou foi assistindo vídeo aula que estão disponíveis ou pesquisando algo que eu não sabia direito. Hoje mesmo eu pesquisei sobre repertório, para saber o que era um bom repertório, já que eu não sabia o que era.",
                      "document": "i02 -.txt",
                      "page": "1",
                      "justification": "O estudante atribui diretamente ao RevisãoOnline o seu conhecimento sobre redação, incluindo a busca por entendimento sobre o que é um bom repertório.",
                      "found_indices": [
                        "572"
                      ],
                      "indicator": "238"
                    },
                    {
                      "text": "também foi uma coisa que veio bastante do revisão online eu lembro da Albilia ensinando lá no primeiro ano só que foi fazendo a minha primeira revisão chegando na parte da proposta de intervenção que eu percebi que as minhas redações nunca tinham uma proposta de intervenção com todas as coisas",
                      "document": "i03 -.txt",
                      "page": "1",
                      "justification": "A estudante relata que percebeu a ausência e a estrutura da proposta de intervenção em seus próprios textos ao utilizar a plataforma.",
                      "found_indices": [
                        "572"
                      ],
                      "indicator": "238"
                    },
                    {
                      "text": "Pelo que eu vi a revisão online ele tem cinco critérios pra proposta de intervenção, que é quem, o quê, como, o efeito e o detalhamento. Eu acho que isso é uma coisa muito interessante, porque justamente isso te ajuda a organizar bastante a tua própria idéia",
                      "document": "i36 - Revisão Online - Matheus.pdf",
                      "page": "7",
                      "justification": "O estudante identifica e valoriza os cinco critérios da proposta de intervenção presentes na plataforma como um auxílio para organizar suas ideias.",
                      "found_indices": [
                        "572"
                      ],
                      "indicator": "238"
                    },
                    {
                      "text": "eu gosto de da forma que está estruturado agora ali no revisão online porque de certa forma também foi como os professores do cursinho cobravam que eles cobravam que a gente tivesse na minha proposta de intervenção eu vou ter que ter essa esses itens então é um checklist até na hora de tu estou escrevendo eu preciso colocar isso",
                      "document": "i37 - Ana Laura.txt",
                      "page": "1",
                      "justification": "A estudante internalizou a estrutura da proposta de intervenção como um 'checklist' para sua escrita, um aprendizado reforçado pela plataforma.",
                      "found_indices": [
                        "572"
                      ],
                      "indicator": "238"
                    },
                    {
                      "text": "No meu ensino fundamental, na minha antiga escola eu não tive nenhuma base de redação, eu nem sabia que, por exemplo, não podia pegar coisas do texto de apoio, que tanto na primeira redação que eu fiz aqui no IF, eu peguei informações do texto de apoio, daí, quando fui fazer uma revisão, no RevisãoOnline, eu descobri que não pode.",
                      "document": "i38 - Bianca.pdf",
                      "page": "1",
                      "justification": "A estudante aprendeu uma regra fundamental do gênero (não usar texto de apoio) através do processo de revisão na plataforma.",
                      "found_indices": [
                        "572"
                      ],
                      "indicator": "238"
                    },
                    {
                      "text": "uma coisa que eu que eu que eu evolui ali com a Fabiana é essa proposta de intervenção né eu não tinha esse antes eu não tinha esse entendimento né com os textos esses do que a gente fez do revisão online eu já meio que montava uma tese já pensando na proposta de intervenção",
                      "document": "i18-Junior.txt",
                      "page": "1",
                      "justification": "O estudante relata uma evolução na sua escrita, passando a articular a tese já com a proposta de intervenção em mente, a partir do uso da plataforma.",
                      "found_indices": [
                        "572"
                      ],
                      "indicator": "238"
                    },
                    {
                      "text": "citações é uma argumentação forte, por que dentro da tua própria argumentação quem tu é? ... por isso eu acho muito importante a citação legitimada por que tu dá força pro argumento.",
                      "document": "i01 - .pdf",
                      "page": "2",
                      "justification": "O estudante demonstra ter compreendido a função argumentativa das citações (repertório) para fortalecer o texto.",
                      "found_indices": [
                        "572"
                      ],
                      "indicator": "238"
                    },
                    {
                      "text": "Foi bem com o RevisãoOnline, eu não sabia antes.",
                      "document": "i16 -.txt",
                      "page": "1",
                      "justification": "O estudante afirma explicitamente que aprendeu sobre a proposta de intervenção, algo que desconhecia antes, através da plataforma.",
                      "found_indices": [
                        "572"
                      ],
                      "indicator": "238"
                    }
                  ],
                  "frequency": 8
                },
                {
                  "name": "Desenvolvimento da Consciência sobre a Materialidade do Texto",
                  "definition": "Esta categoria inclui relatos onde os estudantes demonstram uma elevada consciência e reflexão sobre a própria linguagem e a dos outros. Eles passam a identificar, nomear e monitorar fenômenos linguísticos como repetição de palavras, uso de conectivos (queísmo), e o nível de formalidade, com as ferramentas da plataforma atuando como catalisadoras para este desenvolvimento metalinguístico.",
                  "register_units": [
                    {
                      "text": "o queismo, eu não sabia que existia primeiramente e aí revisando os textos quando chegava na parte do queismo e ele mostrava todos os ques do texto ficava chocada de tanto que que o pessoal bota que eu boto também E aí isso é uma coisa que depois disso eu comecei a reparar e prestar atenção",
                      "document": "i03 -.txt",
                      "page": "1",
                      "justification": "Relato de 'descoberta' do fenômeno do 'queísmo' através da ferramenta, levando a estudante a 'reparar e prestar atenção' em seu uso.",
                      "found_indices": [
                        "573"
                      ],
                      "indicator": "239"
                    },
                    {
                      "text": "comecei a cuidar mais a concordância verbal, que foi o que percebi que estava errando graças ao revisão. Estou dando foco no que tenho mais dificuldade. Foi muito importante",
                      "document": "i08 -GIovana.txt",
                      "page": "1",
                      "justification": "A estudante relata ter começado a 'cuidar' da concordância verbal após perceber seus erros com o auxílio da plataforma.",
                      "found_indices": [
                        "573"
                      ],
                      "indicator": "239"
                    },
                    {
                      "text": "É muito bom enxergar visualmente a repetição de palavras, que é um erro... que é o maior erro, a principal coisa que a gente mais comete, então é bom pra ti ter uma noção de quantas vezes tu repete aquela palavra.",
                      "document": "i09 - Entrevista 11_08_22(Isadora_captions).txt",
                      "page": "1",
                      "justification": "A estudante valoriza a visualização da repetição de palavras fornecida pela plataforma como forma de tomar consciência sobre este erro comum.",
                      "found_indices": [
                        "573"
                      ],
                      "indicator": "239"
                    },
                    {
                      "text": "Voltando a parte do queísmo, por que é algo que eu percebi que eu utilizava bastante e as vez utilizo também, por que é algo que a gente fala no dia-a-dia, é isso que acontece nas redações, muitas vez ocorrem erros por causa do jeito que a gente fala no dia-a-dia, então foi algo que eu li e percebi: 'Caramba! Eu tenho que arrumar alguma forma de cuidar isso",
                      "document": "i12 -Entrevista Laura (120822).pdf",
                      "page": "7",
                      "justification": "A estudante demonstra consciência sobre o 'queísmo' como um vício de oralidade que precisa ser 'cuidado' na escrita, uma percepção ativada pela plataforma.",
                      "found_indices": [
                        "573"
                      ],
                      "indicator": "239"
                    },
                    {
                      "text": "bem legal a primeira vez que o que eu escutei sobre o queismo foi no próprio revisão online que daí só uns dois meses depois a gente viu isso em aula e isso ficou bastante na minha cabeça por causa que eu nunca tinha visto que até muita coisa que eu escrevi eu usava muito que e isso Ficou muito na minha cabeça de todos os tópicos o queismo ele ficou grudado o queismo e o ondismo",
                      "document": "i30 - Entrevista Guilherme.txt",
                      "page": "1",
                      "justification": "O estudante relata que o primeiro contato e a consequente conscientização sobre 'queísmo' e 'ondismo' ocorreram através do RevisãoOnline.",
                      "found_indices": [
                        "573"
                      ],
                      "indicator": "239"
                    },
                    {
                      "text": "o aprendizado do queísmo do ondismo também né a gente usa muito essas essas palavras né e achei interessante isso né a maneira que é que é feito ali o software por Passos",
                      "document": "i18-Junior.txt",
                      "page": "1",
                      "justification": "O estudante menciona o aprendizado sobre 'queísmo' e 'ondismo' como um resultado do uso da ferramenta, indicando uma nova consciência sobre o uso dessas palavras.",
                      "found_indices": [
                        "573",
                        "576"
                      ],
                      "indicator": "239"
                    },
                    {
                      "text": "Os \"que” também.",
                      "document": "i38 - Bianca.pdf",
                      "page": "3",
                      "justification": "Menção sucinta, mas direta, à tomada de consciência sobre o excesso de 'ques' (queísmo) como um ponto de atenção.",
                      "found_indices": [
                        "573"
                      ],
                      "indicator": "239"
                    },
                    {
                      "text": "Principalmente o do queísmo por causa que tem tendem a ter bastante \"que” em um texto. Então é legal você poder ter a marcação de tudo pra ver tipo o que que faz sentido e o que que tá ali só repetindo.",
                      "document": "i36 - Revisão Online - Matheus.pdf",
                      "page": "3",
                      "justification": "O estudante aponta a utilidade da ferramenta de marcação para visualizar e refletir sobre o 'queísmo' e a repetição de palavras.",
                      "found_indices": [
                        "573",
                        "576"
                      ],
                      "indicator": "239"
                    },
                    {
                      "text": "E também no RevisãoOnline eu acho muito importante essa questão das conjunções né, porque o Jonathan tem umas lá que tu acha nossa é bem esse tipo ela é Nossa Tu vai dizer ah a partir disso aí tu acha que é aquilo mas na verdade não tipo é outra coisa diferente, então acho que tem que ser tem que prestar atenção",
                      "document": "i16 -.txt",
                      "page": "1",
                      "justification": "A estudante destaca a importância de 'prestar atenção' no uso correto das conjunções, um aprendizado que atribui à plataforma.",
                      "found_indices": [
                        "573"
                      ],
                      "indicator": "239"
                    }
                  ],
                  "frequency": 9
                },
                {
                  "name": "A Revisão por Pares como Mecanismo de Aprendizagem",
                  "definition": "Esta categoria reúne instâncias onde os estudantes reconhecem explicitamente o ato de revisar o texto de um colega como uma ferramenta poderosa de aprendizagem para a sua própria escrita. O processo de analisar textos alheios, aplicar critérios e identificar erros comuns leva à internalização de regras e ao desenvolvimento de uma perspectiva mais crítica.",
                  "register_units": [
                    {
                      "text": "eu já tinha revisado muito mais já tinha visto erros dos outros para cuidar para não cometer e ver se tu erros meus pensamentos eu escrevo assim mas não fica bom como eu achava que ficava",
                      "document": "i06 -Camila.txt",
                      "page": "1",
                      "justification": "A estudante declara explicitamente que 'ver os erros dos outros' a ajudou a 'cuidar para não cometer' os mesmos erros e a reavaliar sua própria escrita.",
                      "found_indices": [
                        "574"
                      ],
                      "indicator": "240"
                    },
                    {
                      "text": "a gente corrigia outros também e depois que a gente corrigir esses outros era muito mais fácil escrever o nosso né",
                      "document": "i18-Junior.txt",
                      "page": "1",
                      "justification": "O estudante afirma diretamente que após 'corrigir outros textos', a escrita do seu próprio texto se tornou 'muito mais fácil'.",
                      "found_indices": [
                        "574"
                      ],
                      "indicator": "240"
                    },
                    {
                      "text": "Me ajudou em muita coisa assim, perceber os erros de outras pessoas pra conseguir, como posso dizer? Pra não fazer o mesmo erro que os das outras pessoas.",
                      "document": "i38 - Bianca.pdf",
                      "page": "3",
                      "justification": "A estudante reconhece que 'perceber os erros de outras pessoas' foi um mecanismo de aprendizagem para 'não fazer o mesmo erro'.",
                      "found_indices": [
                        "574"
                      ],
                      "indicator": "240"
                    },
                    {
                      "text": "Muitas vezes falta conexão de ideias e queismo acho que é os 2 mais, assim que tu mais via em redações recorrentes.",
                      "document": "i36 - Revisão Online - Matheus.pdf",
                      "page": "8",
                      "justification": "Ao identificar erros recorrentes nos textos dos outros ('conexão de ideias e queismo'), o estudante demonstra um aprendizado que pode ser aplicado em sua própria escrita.",
                      "found_indices": [
                        "574"
                      ],
                      "indicator": "240"
                    },
                    {
                      "text": "as revisões que eram mais completas sempre gerar um aprendizado maior até para tentar realizando na verdade né quando a pessoa que realiza ter essa consciência de que ela aprende mais quer fazer uma revisão bem detalhada né",
                      "document": "i06 -Camila.txt",
                      "page": "1",
                      "justification": "A estudante expressa a consciência de que o ato de realizar uma revisão detalhada para outra pessoa é, em si, um processo que gera mais aprendizado para quem revisa.",
                      "found_indices": [
                        "574"
                      ],
                      "indicator": "240"
                    }
                  ],
                  "frequency": 5
                },
                {
                  "name": "Percepção sobre o Ambiente e as Ferramentas de Revisão",
                  "definition": "Esta categoria engloba as percepções dos estudantes sobre as funcionalidades e o ambiente da plataforma que facilitam ou dificultam a aprendizagem. Inclui a valorização do anonimato para um espaço seguro e imparcial, a utilidade das ferramentas de revisão (automáticas e manuais), e as críticas ou sugestões relacionadas à usabilidade do sistema.",
                  "register_units": [
                    {
                      "text": "mas eu acho que anonimato é melhor porque se fosse se fosse pessoal ainda mais tu mais novo né Você tá no primeiro segundo ano não sei se pode ficar triste com colegas... também tem a questão de que quando ninguém sabe que é tu se sente mais confortável de fazer as coisas então uma pessoa sente melhor de escrever sem ter medo de errar",
                      "document": "i03 -.txt",
                      "page": "1",
                      "justification": "A estudante valoriza o anonimato por criar um ambiente mais confortável e seguro para a escrita, reduzindo o 'medo de errar' e o receio de julgamento entre colegas.",
                      "found_indices": [
                        "575"
                      ],
                      "indicator": "241"
                    },
                    {
                      "text": "eu sinceramente eu acho muito melhor porque te traz ser parcialidade sabe",
                      "document": "i06 -Camila.txt",
                      "page": "1",
                      "justification": "A estudante considera o anonimato 'muito melhor' por garantir a imparcialidade no processo de revisão.",
                      "found_indices": [
                        "575"
                      ],
                      "indicator": "241"
                    },
                    {
                      "text": "Eu acho isso importante, pois de certa formar se tu souber de quem é a redação pode influenciar no teu julgamento quanto aquela análise ali.",
                      "document": "i08 -GIovana.txt",
                      "page": "1",
                      "justification": "A estudante justifica a importância do anonimato para evitar que o julgamento da revisão seja influenciado pelo conhecimento prévio do autor.",
                      "found_indices": [
                        "575"
                      ],
                      "indicator": "241"
                    },
                    {
                      "text": "Eu acho que até é melhor não saber quem avaliou e não saber quem você tá avaliando, por que pode... isso evita que tenham avaliações de má fé.",
                      "document": "i12 -Entrevista Laura (120822).pdf",
                      "page": "9",
                      "justification": "A estudante defende o anonimato como uma forma de evitar 'avaliações de má fé' e garantir a integridade do processo.",
                      "found_indices": [
                        "575"
                      ],
                      "indicator": "241"
                    },
                    {
                      "text": "eu notei que está bem diferente de como era ano passado para mim tem vários outros tipos de erro assim que tu pode colocar e eu achei que ficou bem mais completa agora que dá para especializar bem mais e eu gostei bastante porque ajuda a definir exatamente",
                      "document": "i03 -.txt",
                      "page": "1",
                      "justification": "A estudante percebe a utilidade da maior variedade de 'tipos de erro' (marcações) para fornecer um feedback mais preciso e compreensível.",
                      "found_indices": [
                        "576"
                      ],
                      "indicator": "242"
                    },
                    {
                      "text": "eu gosto dele porque ele te dá um Norte Sempre tu recebe um texto cru assim... mas daí tu aperta assim vem aquela marca aquelas marcações sugeridas daí tu tá agora entendi que onde eu teria começado é um Norte interessante",
                      "document": "i06 -Camila.txt",
                      "page": "1",
                      "justification": "A estudante descreve as 'marcações sugeridas' (revisão automática) como uma ferramenta útil que serve como um 'norte' para iniciar o processo de revisão.",
                      "found_indices": [
                        "576"
                      ],
                      "indicator": "242"
                    },
                    {
                      "text": "E tu acha que aqueles critérios automáticos que o revisão colocou, tu acha que ele ajudou a detectar erros de ortografia, gramática ou algum tipo de erro alguma coisa? xxxx: Sim eles ajudaram bastante na hora de revisar.",
                      "document": "i01 - .pdf",
                      "page": "1",
                      "justification": "O estudante afirma que os 'critérios automáticos' foram úteis para detectar erros diversos durante a revisão.",
                      "found_indices": [
                        "576"
                      ],
                      "indicator": "242"
                    },
                    {
                      "text": "eu achei até muito bem explicadinhas critérios por exemplo, tese tem um # explicando isso Achei muito bacana porque não é todo mundo que sabe, então tu olhando ali vendo uma pequena descrição já sabe a gente vai lá então. achei isso bem fácil de compreender",
                      "document": "i08 -GIovana.txt",
                      "page": "1",
                      "justification": "A estudante valoriza as explicações associadas aos critérios como uma ferramenta útil para compreender o que está sendo avaliado.",
                      "found_indices": [
                        "576"
                      ],
                      "indicator": "242"
                    },
                    {
                      "text": "Eu achei eles bem necessários, eu gostei bastante, porque permite que você mostre pra pessoa onde exatamente ela tem que melhorar. E também o que me auxiliou bastante foi que embaixo de cada um tem um textinho explicando o que cada recurso é.",
                      "document": "i12 -Entrevista Laura (120822).pdf",
                      "page": "2",
                      "justification": "A estudante considera as marcações locais e suas explicações como ferramentas 'bem necessárias' para indicar pontos de melhoria de forma exata.",
                      "found_indices": [
                        "576"
                      ],
                      "indicator": "242"
                    },
                    {
                      "text": "Creio que só mudaria o botão de comentário, pois ele é muito escondido, deixá-lo fixo na página.",
                      "document": "i02 -.txt",
                      "page": "1",
                      "justification": "Crítica de usabilidade sobre um elemento da interface ('botão de comentário') estar 'muito escondido', com uma sugestão de melhoria.",
                      "found_indices": [
                        "577"
                      ],
                      "indicator": "243"
                    },
                    {
                      "text": "Na primeira vez que a nossa turma foi usar o meu não tava funcionando. Apareceu já uma revisão, não era pra tê nenhuma, apareceu uma revisão que eu nem consegui terminar e que o site trava... devia tá com erro e tudo mais...",
                      "document": "i09 - Entrevista 11_08_22(Isadora_captions).txt",
                      "page": "1",
                      "justification": "Relato de problemas técnicos e de funcionamento da plataforma, como bugs e travamentos ('site trava').",
                      "found_indices": [
                        "577"
                      ],
                      "indicator": "243"
                    },
                    {
                      "text": "eu queria muito falar sobre essa opção de voltar que eu acho que foi a coisa mais frustrante do revisãoonline para mim quando tu volta tu não ele apaga as informações que tu botou",
                      "document": "i30 - Entrevista Guilherme.txt",
                      "page": "1",
                      "justification": "Crítica severa a um problema de usabilidade no fluxo de navegação, onde o botão 'voltar' apaga o trabalho feito.",
                      "found_indices": [
                        "577"
                      ],
                      "indicator": "243"
                    },
                    {
                      "text": "quando marcava assim por cima não dava para marcar por cima né eu marcava uma palavra daí depois eu queria marcar o parágrafo eu não consegui Daí tive uma dificuldade de marcar",
                      "document": "i25 - Gabriela.txt",
                      "page": "1",
                      "justification": "Relato de uma dificuldade de usabilidade com a ferramenta de marcação, que não permitia sobrepor anotações, gerando atrito na experiência.",
                      "found_indices": [
                        "577"
                      ],
                      "indicator": "243"
                    },
                    {
                      "text": "teve duas coisas que eu tive duas categorias não é de elementos coesivos e outra é a categoria de conjunções e na realidade conjunções são elementos coesivos e daí eu fiquei tipo meio assim o que que eu assim achei Então desnecessário.",
                      "document": "i37 - Ana Laura.txt",
                      "page": "1",
                      "justification": "Crítica sobre a organização dos critérios, apontando uma redundância ('conjunções são elementos coesivos') que tornou a classificação confusa e 'desnecessária'.",
                      "found_indices": [
                        "577"
                      ],
                      "indicator": "243"
                    }
                  ],
                  "frequency": 14
                }
              ]
            }
            ```
            """;
    }
}

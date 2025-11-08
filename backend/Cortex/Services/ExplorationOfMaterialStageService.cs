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
                                                { "line", new OpenApiSchema { Type = Type.String, Description = "Número da linha" } },
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
                                            Required = { "text", "document", "page", "line", "justification", "found_indices", "indicator" }
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
    
    COMPREENSÃO IMPORTANTE:
    NÃO existe NECESSARIAMENTE uma relação 1:1 entre índices e categorias!
    - Índices são elementos concretos no texto (palavras, frases, expressões)
    - Unidades de registro são segmentos de texto que contêm um ou mais índices
    - Categorias são agrupamentos temáticos que emergem de múltiplas unidades de registro
    
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
              "page": "2",
              "line": "8",
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
              "line": "15",
              "found_indices": ["idx_001"],
              "main_indicator": "idx_001", 
              "justification": "Relata problema técnico que prejudicou a aula"
            }},
            {{
              "text": "Me sentia completamente esgotado, não tinha energia para preparar aulas",
              "document": "entrevista_prof_01.pdf",
              "page": "3",
              "line": "22",
              "found_indices": ["idx_002"],
              "main_indicator": "idx_002",
              "justification": "Expressa exaustão relacionada ao trabalho remoto"
            }},
            {{
              "text": "O Zoom travou e perdi todos os alunos da sala",
              "document": "entrevista_prof_03.pdf",
              "page": "1",
              "line": "8",
              "found_indices": ["idx_001"],
              "main_indicator": "idx_001",
              "justification": "Outro problema técnico que impactou a aula"
            }},
            {{
              "text": "Sozinho na frente da tela, sem ver o rosto de ninguém, me sentia isolado",
              "document": "entrevista_prof_02.pdf",
              "page": "4",
              "line": "30",
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
              "line": "12",
              "found_indices": ["idx_004"],
              "main_indicator": "idx_004",
              "justification": "Relata aprendizado de competências tecnológicas"
            }},
            {{
              "text": "Trabalhando de casa, tive mais tempo com minha família",
              "document": "entrevista_prof_04.pdf",
              "page": "3",
              "line": "18",
              "found_indices": ["idx_005"],
              "main_indicator": "idx_005",
              "justification": "Identifica benefício relacionado à flexibilidade"
            }},
            {{
              "text": "Me capacitei em ferramentas digitais que nunca tinha usado",
              "document": "entrevista_prof_01.pdf",
              "page": "5",
              "line": "40",
              "found_indices": ["idx_004"],
              "main_indicator": "idx_004",
              "justification": "Outro relato de desenvolvimento de competências"
            }},
            {{
              "text": "Evitar duas horas de trânsito por dia foi libertador",
              "document": "entrevista_prof_03.pdf",
              "page": "2",
              "line": "25",
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
    1. Amor romântico
    2. Casamento estabelecido
    3. Idealismo
    B / Intenções na "cabeça"
    1. Solução de problemas concretos
    2. Progresso pessoal
    3. Dinheiro e bens materiais
    4.
    5.
    6.
    7.
    8.
    Afeição e segurança emocional 4, Segurança econômica e social
    Poder e dominaçãoPatriotismo
    Aventura
    Justiça
    Independência
    5.
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
            {
              "categories": [
                {
                  "name": "Fundamentos Pedagógicos Humanizados",
                  "definition": "Agrupa as unidades de registro que descrevem uma abordagem pedagógica centrada na humanização do ensino, valorizando a relação afetiva, o tratamento positivo do erro e a criação de um ambiente de aprendizagem acolhedor e motivador.",
                  "register_units": [
                    {
                      "text": "De afetividade, ponto! Não tem outra (risos)... acho que deve ser isto em qualquer ambiente de aprendizagem, é o principal...",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "4",
                      "line": "1",
                      "justification": "A unidade de registro expressa a centralidade da afetividade no processo de aprendizagem, alinhando-se diretamente ao índice de Relação Afetiva e Humanizada.",
                      "found_indices": [
                        "272"
                      ],
                      "indicator": "40"
                    },
                    {
                      "text": "(risos) de amizade!!! (risos) né? É o que era mais legal das aulas é que a gente se divertia.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "2",
                      "line": "19",
                      "justification": "O trecho destaca a amizade e a diversão como elementos centrais da aula, caracterizando um ambiente de Relação Afetiva e Humanizada.",
                      "found_indices": [
                        "272"
                      ],
                      "indicator": "40"
                    },
                    {
                      "text": "eu descreveria como uma metodologia mais contemporânea, mais humana né, não, sei lá, nada a ver com a varetinha que se acredita que aprende, mas mais humana, mais, mais próxima.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "23",
                      "line": "33",
                      "justification": "A entrevistada define a metodologia como 'humana' e 'próxima', em contraste com métodos tradicionais, o que corresponde ao índice de Relação Afetiva e Humanizada.",
                      "found_indices": [
                        "272"
                      ],
                      "indicator": "40"
                    },
                    {
                      "text": "mas o ambiente era bom, era tranquilo...",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "24",
                      "line": "4",
                      "justification": "A descrição do ambiente como 'bom' e 'tranquilo' é um claro indicador de uma Relação Afetiva e Humanizada.",
                      "found_indices": [
                        "272"
                      ],
                      "indicator": "40"
                    },
                    {
                      "text": "é muito...tranquila, muito tranquila... não tinha aquela rigidez formal que às vezes distaciada que alguns professores gostam de impor",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "10",
                      "line": "1",
                      "justification": "A ausência de rigidez formal e a tranquilidade do ambiente são características que se enquadram na definição do índice de Relação Afetiva e Humanizada.",
                      "found_indices": [
                        "272"
                      ],
                      "indicator": "40"
                    },
                    {
                      "text": "a gente sempre foi muito, muito amigas... muito, muito amigas. Desde os cinco anos de idade até sempre. A gente sempre foi muito junta e unida, a nossa turma sempre foi assim.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "33",
                      "line": "32",
                      "justification": "O relato de uma relação de amizade forte e duradoura entre professora e alunas exemplifica o pilar da Relação Afetiva e Humanizada.",
                      "found_indices": [
                        "272"
                      ],
                      "indicator": "40"
                    },
                    {
                      "text": "como eu ria muito dos meus erros e eu adoro os meus erros (risos)... e eu acho que era isso assim.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "2",
                      "line": "21",
                      "justification": "A unidade de registro demonstra uma atitude positiva e de humor em relação aos próprios erros, alinhada ao índice de Tratamento Pedagógico do Erro.",
                      "found_indices": [
                        "273"
                      ],
                      "indicator": "41"
                    },
                    {
                      "text": "(risos) era um divertimento né? (risos) não tem outra explicação.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "2",
                      "line": "26",
                      "justification": "A descrição do erro como 'divertimento' captura a essência do índice de Tratamento Pedagógico do Erro, que vê o erro como algo leve e natural.",
                      "found_indices": [
                        "273"
                      ],
                      "indicator": "41"
                    },
                    {
                      "text": "em questão dos erros assim eu não me sentia pressionada pela professora",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "2",
                      "line": "28",
                      "justification": "A ausência de pressão ao errar é um indicador direto de um Tratamento Pedagógico do Erro que não é punitivo.",
                      "found_indices": [
                        "273"
                      ],
                      "indicator": "41"
                    },
                    {
                      "text": "eu não me lembro do erro me traumatizar, não não, não me traumatizava, não me causava angústia, era uma coisa que eu errava e tentava ter consciência para não fazer de novo, mas ele não me traumatizava.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "24",
                      "line": "8",
                      "justification": "Este trecho descreve a ausência de trauma ou angústia associada ao erro, o que caracteriza um Tratamento Pedagógico do Erro positivo e construtivo.",
                      "found_indices": [
                        "273"          ],
                      "indicator": "41"
                    },
                    {
                      "text": "o erro era tratado com naturalidade, porque ele faz parte né",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "9",
                      "line": "9",
                      "justification": "A afirmação de que o erro era 'tratado com naturalidade' e 'faz parte' corresponde exatamente à descrição do índice de Tratamento Pedagógico do Erro.",
                      "found_indices": [
                        "273"
                      ],
                      "indicator": "41"
                    },
                    {
                      "text": "ninguém ali ia morrer, se atirar pela janela porque não levantou a perna na cabeça, sabe?",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "9",
                      "line": "13",
                      "justification": "A expressão hiperbólica e bem-humorada ilustra a leveza com que o erro era tratado, diminuindo a ansiedade e a pressão, conforme o índice 273.",
                      "found_indices": [
                        "273"
                      ],
                      "indicator": "41"
                    }
                  ],
                  "frequency": 12
                },
                {
                  "name": "Ressignificação da Técnica do Balé Clássico",
                  "definition": "Reúne as manifestações que apontam para uma desconstrução da visão tradicional do Balé, enfatizando a descoberta do corpo, a consciência somática e a integração entre teoria e prática, em oposição a uma abordagem puramente sacrificial ou reprodutiva.",
                  "register_units": [
                    {
                      "text": "eram aulas que contribuíam muito pra eu descobrir outras coisas possibilidades do meu corpo que eu não conhecia assim.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "1",
                      "line": "21",
                      "justification": "O trecho aponta para a 'descoberta' de possibilidades do corpo, alinhando-se ao índice da Pedagogia da Descoberta e do Prazer.",
                      "found_indices": [
                        "270"
                      ],
                      "indicator": "38"
                    },
                    {
                      "text": "uma ideia muito fechada do Balé de ver, de sacrifício, e de... assim como eu posso dizer, uma...aquela coisa que Balé maltrata o corpo, não! E Eu não me sentia assim!!",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "1",
                      "line": "23",
                      "justification": "A unidade de registro contrasta a experiência da aula com a visão tradicional do Balé como 'sacrifício', o que é central para o índice 270.",
                      "found_indices": [
                        "270"
                      ],
                      "indicator": "38"
                    },
                    {
                      "text": "Ah eu acho que eu desconstruí a ideia que eu tinha do Balé, principalmente! A ideia que eu tinha da coisa chata, da coisa sacrificante",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "1",
                      "line": "41",
                      "justification": "A desconstrução da ideia do Balé como 'coisa chata' e 'sacrificante' é um indicador explícito da Pedagogia da Descoberta e do Prazer.",
                      "found_indices": [
                        "270"
                      ],
                      "indicator": "38"
                    },
                    {
                      "text": "depois (risos) que eu fiz Balé com a Mônica eu vi que não precisa ser sacrificante, não precisa ser isso...",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "2",
                      "line": "14",
                      "justification": "A afirmação 'não precisa ser sacrificante' é um termo-chave do indicador 38, ressignificando a prática do Balé.",
                      "found_indices": [
                        "270"
                      ],
                      "indicator": "38"
                    },
                    {
                      "text": "e ai eu tinha prazer... aí depois de um mês, dois meses, eu já sabia o que ia acontecer, já ficava mais tranquila... e aí tu faz aquilo com prazer",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "2",
                      "line": "7",
                      "justification": "A presença da palavra 'prazer' para descrever a experiência da aula é um indicador direto do índice 270.",
                      "found_indices": [
                        "270"
                      ],
                      "indicator": "38"
                    },
                    {
                      "text": "eu percebi que o balé não precisa ser uma coisa tão séria quanto ele parece ser. O balé pode ser uma coisa feliz, ele não po... ele não precisa ser uma coisa monótona",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "35",
                      "line": "10",
                      "justification": "Este trecho contrapõe a seriedade e monotonia com a felicidade, desconstruindo a visão rígida do Balé e alinhando-se à Pedagogia do Prazer.",
                      "found_indices": [
                        "270"
                      ],
                      "indicator": "38"
                    },
                    {
                      "text": "o que me faltava é o que o Balé dá: é a consciência corporal que o Balé dá, ã, a noção cinesiológica que o Balé dá, anatômica que o Balé dá",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "7",
                      "line": "26",
                      "justification": "A unidade de registro lista explicitamente os termos 'consciência corporal', 'cinesiológica' e 'anatômica' como aprendizados, correspondendo ao índice de Consciência Corporal e Somática.",
                      "found_indices": [
                        "271"
                      ],
                      "indicator": "39"
                    },
                    {
                      "text": "o Balé, ele é todo explicadinho, ãaa relacionado né, À Anatomia, Fisiologia, Cinesiologia, tudo né, certinho.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "7",
                      "line": "35",
                      "justification": "A menção à relação do Balé com 'Anatomia' e 'Cinesiologia' evidencia o foco no entendimento do corpo, característico do índice 271.",
                      "found_indices": [
                        "271"
                      ],
                      "indicator": "39"
                    },
                    {
                      "text": "o Balé o que faz? É te proteger, ele te protege",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "7",
                      "line": "39",
                      "justification": "A ideia de que a técnica do Balé serve para 'proteger o corpo' é um pilar do índice de Consciência Corporal e Somática.",
                      "found_indices": [
                        "271"
                      ],
                      "indicator": "39"
                    },
                    {
                      "text": "Eu aprendi a ter uma melhor consciência do meu corpo, a ter uma melhor aaaa... movimentação do meu corpo",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "10",
                      "line": "10",
                      "justification": "O relato de aprendizado da 'consciência do meu corpo' é uma manifestação direta do índice 271.",
                      "found_indices": [
                        "271"
                      ],
                      "indicator": "39"
                    },
                    {
                      "text": "lembro que tu dava claro Balé, e tem umas coisas da... como é que chama aquilo? Educação Somática(...)",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "7",
                      "line": "48",
                      "justification": "A identificação de elementos da 'Educação Somática' nas aulas de Balé conecta a prática diretamente ao índice de Consciência Corporal e Somática.",
                      "found_indices": [
                        "271"
                      ],
                      "indicator": "39"
                    },
                    {
                      "text": "eu me lembro de abordagem teórica no sentido de explicar pra que que é aquilo...né, ãaa, por exemplo, porque que a gente tem que contrair o glúteo para ter equilíbrio",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "4",
                      "line": "13",
                      "justification": "A descrição da professora explicando o 'pra que que é aquilo' (o propósito do movimento) exemplifica a Integração Teoria-Prática.",
                      "found_indices": [
                        "275"
                      ],
                      "indicator": "43"
                    },
                    {
                      "text": "tu explicava o movimento a gente ia fazer de uma forma bem simples, e a gente sabia pra que que era aquilo, pra que que poderia servir aquilo.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "4",
                      "line": "18",
                      "justification": "Este trecho mostra que a professora 'explicava o movimento' e seu propósito ('pra que que era aquilo'), o que define o índice de Integração Teoria-Prática.",
                      "found_indices": [
                        "275"
                      ],
                      "indicator": "43"
                    },
                    {
                      "text": "tu sempre explicava como era, como se escreve né, pra saber, pra ensinar, acho que isso eu acabei levando...",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "23",
                      "line": "18",
                      "justification": "A menção à explicação de 'como se escreve' o passo (nomenclatura) é um indicador da abordagem teórica vinculada à prática, conforme o índice 275.",
                      "found_indices": [
                        "275"
                      ],
                      "indicator": "43"
                    },
                    {
                      "text": "ter este momento de trazer a teoria ajuda a tu compreender a importância de se fazer aquilo ali do jeito que tem que ser feito e não fazer de qualquer jeito",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "11",
                      "line": "12",
                      "justification": "A unidade de registro valoriza o 'trazer a teoria' para dar sentido e corrigir a execução do movimento, o que é a essência do índice de Integração Teoria-Prática.",
                      "found_indices": [
                        "275"
                      ],
                      "indicator": "43"
                    }
                  ],
                  "frequency": 15
                },
                {
                  "name": "Estratégias Metodológicas Flexíveis e Lúdicas",
                  "definition": "Congrega os trechos que evidenciam o uso de estratégias metodológicas que se afastam da rigidez, incluindo a adaptação aos diferentes contextos e alunos, e o emprego da ludicidade como ferramenta central para o engajamento e a aprendizagem.",
                  "register_units": [
                    {
                      "text": "E quando tu ia dando as aulas, tu já ia explicando pro pessoal ali como é que dava pra adaptar aquilo pra outros contextos",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "7",
                      "line": "50",
                      "justification": "O relato de que a professora explicava como 'adaptar aquilo pra outros contextos' corresponde diretamente ao índice de Adaptação e Flexibilidade Metodológica.",
                      "found_indices": [
                        "274"
                      ],
                      "indicator": "42"
                    },
                    {
                      "text": "tu respeitavas o, o tempo de desenvolvimento de cada um. E isso é uma coisa boa também, porque dentro de uma turma tu pode ter alguém super avançado... e alguém que nunca fez nada",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "8",
                      "line": "11",
                      "justification": "A menção a 'respeitavas o tempo de cada um' e a consideração dos diferentes níveis na turma são indicadores claros de Adaptação e Flexibilidade Metodológica.",
                      "found_indices": [
                        "274"
                      ],
                      "indicator": "42"
                    },
                    {
                      "text": "aprendizagem de repente de culturas novas né porque tu traz coisas assim de outras danças, de coisas que tavam passando na TV...",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "25",
                      "line": "2",
                      "justification": "A incorporação de elementos de 'outras danças' e da cultura contemporânea ('TV') demonstra uma abordagem aberta e flexível, alinhada ao índice 274.",
                      "found_indices": [
                        "274"
                      ],
                      "indicator": "42"
                    },
                    {
                      "text": "sempre teve aquela ideia da gente brincar assim",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "3",
                      "line": "50",
                      "justification": "A presença da ideia de 'brincar' como um elemento constante nas aulas é um indicador do índice de Ludicidade como Ferramenta Pedagógica.",
                      "found_indices": [
                        "277"
                      ],
                      "indicator": "45"
                    },
                    {
                      "text": "eu acho que tem que ter essa leveza que o Balé sabe traz assim historicamente, que não tem...",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "3",
                      "line": "55",
                      "justification": "A valorização da 'leveza' na prática do Balé, em contraste com a rigidez, aponta para o uso de elementos lúdicos e uma abordagem menos solene.",
                      "found_indices": [
                        "277"
                      ],
                      "indicator": "45"
                    },
                    {
                      "text": "a gente também fazia no final um teatrinho que a gente tinha que fazer passos de balé e essas coisas.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "32",
                      "line": "14",
                      "justification": "O uso de 'teatrinho' como atividade de aula é um exemplo explícito de Ludicidade como Ferramenta Pedagógica.",
                      "found_indices": [
                        "277"
                      ],
                      "indicator": "45"
                    },
                    {
                      "text": "Eu lembro uma que eu nunca mais vou esquecer que é do tubarão que tu desenhava no chão e era pra gente pular. Eu lembro de uma vez também que a gente fez, que tu fez um castelo e a gente tinha que ir passando pelos desafios do castelo pra gente poder chegar no chá das princesas.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "32",
                      "line": "23",
                      "justification": "A descrição de atividades imaginativas e narrativas, como 'tubarão' e 'castelo', são exemplos concretos do uso da ludicidade no ensino.",
                      "found_indices": [
                        "277"
                      ],
                      "indicator": "45"
                    },
                    {
                      "text": "tu não vai encontra outro curso onde a professora brinca e conversa direito contigo.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "33",
                      "line": "27",
                      "justification": "A palavra 'brinca' é usada para caracterizar o modo de ensinar da professora, o que corresponde diretamente ao índice 277.",
                      "found_indices": [
                        "277",
                        "272"
                      ],
                      "indicator": "45"
                    },
                    {
                      "text": "Então eu acho que esse jeito teu de ensinar, esse jeito teu de brincar, me estimula a continuar dançando balé até hoje.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "35",
                      "line": "10",
                      "justification": "O 'jeito de brincar' é identificado como um fator de estímulo e permanência na dança, validando a ludicidade como uma ferramenta pedagógica eficaz.",
                      "found_indices": [
                        "277"
                      ],
                      "indicator": "45"
                    }
                  ],
                  "frequency": 9
                },
                {
                  "name": "A Formação Docente pela Práxis Colaborativa",
                  "definition": "Categoria que agrupa as vivências de formação da professora baseadas na prática compartilhada e na mentoria, indicando um modelo de 'aprender fazendo' e 'aprender com o outro' que se reflete em sua própria metodologia pedagógica.",
                  "register_units": [
                    {
                      "text": "eu já comecei a dar aula de Balé de forma compartilhada... Desdo primeiro ano que eu comecei a dar aula lá em 1990, ãa, já foi com essa proposta de trabalhar junto com a Aline Peres",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "13",
                      "line": "32",
                      "justification": "O trecho descreve o início da carreira docente da professora como uma experiência 'compartilhada' e de 'trabalhar junto', alinhando-se ao índice de Formação pela Experiência e Colaboração.",
                      "found_indices": [
                        "276"
                      ],
                      "indicator": "44"
                    },
                    {
                      "text": "e ao mesmo tempo com uma supervisão, vamos dizer assim, da Tia Beth, que dava todo o suporte, né... da gente di di dá ideias e de... e da gente fazer aquela... tinha aquela rotina da gente montar a aula lá e os exercícios, de mostrar pra ela antes, pra ela dar alguma dica, algum ajuste",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "13",
                      "line": "40",
                      "justification": "A descrição do processo de 'supervisão' e mentoria por uma professora mais experiente é um indicador claro de Formação pela Experiência e Colaboração.",
                      "found_indices": [
                        "276"
                      ],
                      "indicator": "44"
                    },
                    {
                      "text": "Eu acho Fun da men tal !! (fala pausada e enfática) Eu acho que a gente aprende com o outro, a gente ensina o outro.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "14",
                      "line": "21",
                      "justification": "Esta unidade de registro expressa a crença fundamental de que se 'aprende com o outro', que é a base do índice de Formação pela Experiência e Colaboração.",
                      "found_indices": [
                        "276"
                      ],
                      "indicator": "44"
                    },
                    {
                      "text": "além dessa, dessa conversa anterior a aula, ãa sim, a Tia Beth assistia as aulas da gente assim, assistia e se não assistia por inteiro entrava num pedaço, entrava dava uma olhada... tinha essa figura que supervisionava... tinha.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "15",
                      "line": "45",
                      "justification": "O relato da existência de uma 'figura que supervisionava' as aulas reforça a ideia de uma formação docente baseada na prática supervisionada e na colaboração.",
                      "found_indices": [
                        "276"
                      ],
                      "indicator": "44"
                    },
                    {
                      "text": "Eu boto uma pessoa dando aula e outra pessoa de auxiliar. De auxiliar, pra te ajudar. Se um dia tu não puderes vir, essa pessoa que ta te ajudando, eu entro na sala com ela... ela tem capacidade de dar aula e sabe onde anda o programa. (COLABORAÇÃO)",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "44",
                      "line": "17",
                      "justification": "A descrição da dinâmica de 'professora e uma auxiliar' como um sistema de apoio e colaboração é um exemplo prático do índice de Formação pela Experiência e Colaboração.",
                      "found_indices": [
                        "276"
                      ],
                      "indicator": "44"
                    }
                  ],
                  "frequency": 5
                }
              ]
            }
            """;
    }
}

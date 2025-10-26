using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Repositories;
using Cortex.Repositories.Interfaces;
using Cortex.Services.Interfaces;
using System.Text;
using System.Text.Json;

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
    1. Use APENAS os índices e indicadores fornecidos (referencie pelo ID)
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
            string jsonResponse = GetMockedGeminiResponse();
            //deixei comentado por enquanto pra não gastar recurso
            //string jsonResponse = await _geminiService.GenerateContentWithDocuments(documentInfos, finalPrompt);

            _logger.LogInformation("Resposta recebida do Gemini com sucesso.");

            resultBaseClass.PromptResult = jsonResponse;

            GeminiCategoryResponse geminiResponse = _geminiResponseHandler.ParseResponse<GeminiCategoryResponse>(jsonResponse);
          
            ExplorationOfMaterialStage stageEntity = await _explorationPersistenceService.MapAndSaveExplorationResultAsync(analysis.Id, geminiResponse, allDocuments);

            resultBaseClass.ExplorationOfMaterialStage = stageEntity;
            resultBaseClass.PromptResult = jsonResponse;
            resultBaseClass.IsSuccess = true;
            _logger.LogInformation("========== EXPLORAÇÃO DO MATERIAL CONCLUÍDA COM SUCESSO ==========");

            return resultBaseClass;
        }
        catch (JsonException jsonEx)
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
                  "name": "Saberes Constituintes da Prática Pedagógica",
                  "definition": "Agrupa unidades de registro que revelam as fontes de conhecimento da professora, incluindo saberes pedagógicos formais, o conhecimento advindo da própria experiência corporal e as vivências (especialmente as negativas) que serviram de contraponto para a construção de sua metodologia.",
                  "frequency": 15,
                  "register_units": [
                    {
                      "text": "Mas aí eu te digo né Mônica, a metodologia, da coisa, entendesse. Porque não é qualquer um que sabe fazer aquilo, sem ter, sem ficar, ãaaa. Ah não sei se eu vou saber explicar, mas sem sem sem ficar com aquilo... porque eu lembro assim ó, que a gente fazia aquela aula de Balé, mas era uma coisa desconstruída já",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "2",
                      "line": "53",
                      "found_indices": ["199", "202"],
                      "indicator": "16",
                      "justification": "A unidade de registro aponta para a existência de uma 'metodologia' específica e um saber-fazer que a diferencia, além de caracterizá-la como 'desconstruída', alinhando-se aos saberes pedagógicos que fundamentam a prática."
                    },
                    {
                      "text": "aquela técnica é necessária pra qualquer estilo, porém, a metodologia, tem que ser muito bem pensada, pra não ficar uma coisa muito assim sabe, é assim!",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "3",
                      "line": "1",
                      "found_indices": ["199"],
                      "indicator": "16",
                      "justification": "O trecho enfatiza a importância da 'metodologia' como um saber pedagógico que qualifica a aplicação da técnica, sendo um pilar na construção da prática da professora."
                    },
                    {
                      "text": "não era aquilo que eu já tinha visto... assim eu já tinha tido umas experiências não muito boas!",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "3",
                      "line": "6",
                      "found_indices": ["200"],
                      "indicator": "17",
                      "justification": "A fala evidencia como experiências formativas anteriores, percebidas como negativas ('não muito boas'), servem de contraponto para a metodologia atual, constituindo um saber experiencial por oposição."
                    },
                    {
                      "text": "primeiro, a metodologia que a pessoa pensa em fazer isso, primeiro, ela tem que ter noção dos vários estilos que pode trabalhar dentro daquela, daquele contexto do Balé, né?",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "3",
                      "line": "21",
                      "found_indices": ["199"],
                      "indicator": "16",
                      "justification": "A unidade destaca a 'metodologia' e a 'noção' de diferentes abordagens como um saber pedagógico fundamental para a prática docente, anterior à própria ação de ensinar."
                    },
                    {
                      "text": "Então seria uma metodologia de trabalhar o Balé de forma universal assim, né. Enxergando qualquer possibilidade ali, ta aberta de repente por aluno também né?",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "3",
                      "line": "34",
                      "found_indices": ["199"],
                      "indicator": "16",
                      "justification": "A percepção da aluna sobre uma 'metodologia' de ensino 'universal' e 'aberta' reflete um saber pedagógico que estrutura a prática da professora, focando na adaptabilidade e na pertinência para o aluno."
                    },
                    {
                      "text": "Ah eu me lembro de abordagem teórica no sentido de explicar pra que que é aquilo...né, ãaa, por exemplo, porque que a gente tem que contrair o glúteo para ter equilíbrio e... sempre tu dizia",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "4",
                      "line": "17",
                      "found_indices": ["199"],
                      "indicator": "16",
                      "justification": "O relato descreve a intencionalidade pedagógica de 'explicar pra que que é aquilo', um saber didático que busca dar sentido e funcionalidade aos movimentos, ultrapassando a mera reprodução técnica."
                    },
                    {
                      "text": "então sempre tinha esta explicação. Eu me lembro que sempre tinha, tu explicava o movimento a gente ia fazer de uma forma bem simples, e a gente sabia pra que que era aquilo, pra que que poderia servir aquilo.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "4",
                      "line": "21",
                      "found_indices": ["199"],
                      "indicator": "16",
                      "justification": "Esta unidade reforça a presença de um saber didático explícito, onde a professora se preocupa em dar sentido ('sabia pra que que era aquilo') ao aprendizado, um pilar de sua pedagogia."
                    },
                    {
                      "text": "Não me faz de pateta(risos)! Então assim, tem que ter um sentido. Eu acho que tu sempre tentou em todos os movimentos pra que que é isso então, então tu fazia primeiro e também explicava, esta era a parte teórica assim.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "4",
                      "line": "28",
                      "found_indices": ["199"],
                      "indicator": "16",
                      "justification": "A busca por 'sentido' em todos os movimentos é um forte indicador do saber pedagógico, que transforma a aula em uma experiência de aprendizado consciente e não apenas de execução."
                    },
                    {
                      "text": "era uma coisa até humilhante assim, né, pra quem não conseguia entender um plié, um... qualquer coisa mais simples, assim que a pessoa não tava, nunca tinha visto, não tava acostumada, então eu fiquei com essa experiência assim ó de ser uma coisa \"se tu não fizer isso tu não é nada\"!",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "4",
                      "line": "35",
                      "found_indices": ["200"],
                      "indicator": "17",
                      "justification": "A memória de uma experiência 'humilhante' em outra aula de balé serve como um saber formativo por contraponto, influenciando a professora a construir uma prática que evite esse tipo de abordagem."
                    },
                    {
                      "text": "mas eu me lembro dele humilhar muito as outras meninas assim, e isso me traumatizou um pouco assim (risos).",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "4",
                      "line": "44",
                      "found_indices": ["200"],
                      "indicator": "17",
                      "justification": "O relato de 'trauma' e 'humilhação' vivenciados como observadora em outra experiência formativa constitui uma fonte de saber que, por oposição, fundamenta a busca por uma pedagogia afetiva e respeitosa."
                    },
                    {
                      "text": "tinha algumas coisas que eu identificava ali que eram também de Educação Somática e que tinha a vê, total assim! E quando tu ia dando as aulas, tu já ia explicando pro pessoal ali como é que dava pra adaptar aquilo pra outros contextos, como é que eles tinham que pensar com os alunos deles",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "7",
                      "line": "44",
                      "found_indices": ["199"],
                      "indicator": "16",
                      "justification": "A unidade aponta para um saber pedagógico que transcende a aula de balé, instrumentalizando os alunos (futuros professores) para a 'formação de professores' ao explicar como 'adaptar' o conteúdo."
                    },
                    {
                      "text": "A resposta, (risos) aprender (risos)! Ãaa ah é que eles tivessem uma consciência corporal dentro do seu estágio de desenvolvimento, de consciência corporal a partir duma técnica de Balé que eles tivessem compreendendo a importância de cada etapa daquelas ali, porque que tinha que ser daquele jeito",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "10",
                      "line": "40",
                      "found_indices": ["199"],
                      "indicator": "16",
                      "justification": "A expectativa da professora, segundo o aluno, era a compreensão da 'importância' e do 'porquê' de cada etapa, o que reflete um saber pedagógico focado na construção de sentido e não na simples reprodução."
                    },
                    {
                      "text": "A professora aaa, (risos) ãa eu acho que ela é competente, didaticamente experiente e competente, ao ponto de ter vivenciado muito todas estas técnicas no seu corpo, e por compreender, ter compreendido isso tanto no seu corpo, sabe ensinar no corpo dos outros, porque já identificou os problemas que os outros tão passando ali ela consegue identificar no corpo dos outros porque ela já passou por aquilo ali em alguma outra..",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "11",
                      "line": "31",
                      "found_indices": ["204"],
                      "indicator": "21",
                      "justification": "Esta unidade é um exemplo claro do corpo próprio como fonte de saber, onde a vivência das técnicas e dificuldades no próprio corpo se transforma em empatia e competência didática para 'ensinar no corpo dos outros'."
                    },
                    {
                      "text": "Então até as experiências ruins que tu viveste, dava pra ver que tu soubeste puxar disso, dessas experiências pra transformar pra o momento que tu fosses ser professora, tu não causasse esse sofrimento que tu passou nos teus alunos",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "12",
                      "line": "1",
                      "found_indices": ["200"],
                      "indicator": "17",
                      "justification": "O trecho explicita como as 'experiências ruins' e o 'sofrimento' do passado foram transformados em um saber pedagógico, um contraponto consciente para evitar a repetição de práticas negativas com seus próprios alunos."
                    },
                    {
                      "text": "Então eu acho que todas as dificuldades te fizeram buscar isso pro momento que tu fosse ensinar, tu saber ajudar os outros a ultrapassarem os limites que tu demorou bastante pra ultrapassar, tu seria uma facilitadora, neste sentido...",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "12",
                      "line": "12",
                      "found_indices": ["204"],
                      "indicator": "21",
                      "justification": "A fala conecta diretamente as 'dificuldades' vivenciadas no corpo da professora com a sua capacidade de 'ajudar os outros a ultrapassarem os limites', caracterizando o corpo próprio como uma fonte de saber empático e pedagógico."
                    }
                  ]
                },
                {
                  "name": "Metodologia de Ensino Centrada no Aluno",
                  "definition": "Reúne unidades de registro que descrevem a aplicação prática da metodologia da professora, caracterizada pelo cuidado com o corpo, a desconstrução de estereótipos do balé, a criação de um ambiente afetivo e lúdico, e o estímulo à prática reflexiva e criativa dos alunos.",
                  "frequency": 37,
                  "register_units": [
                    {
                      "text": "eram aulas que contribuíam muito pra eu descobrir outras coisas possibilidades do meu corpo que eu não conhecia assim.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "1",
                      "line": "21",
                      "found_indices": ["201"],
                      "indicator": "18",
                      "justification": "A unidade descreve a aula como um espaço para a descoberta de 'possibilidades do corpo', alinhando-se a uma metodologia que promove a consciência corporal e o respeito ao corpo."
                    },
                    {
                      "text": "eu tive outra visão do Balé, entendesse, eu tinha uma coisa muito fechada assim, uma ideia muito fechada do Balé de ver, de sacrifício, e de... assim como eu posso dizer, uma...aquela coisa que Balé maltrata o corpo, não! E Eu não me sentia assim!!",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "1",
                      "line": "23",
                      "found_indices": ["202", "200"],
                      "indicator": "19",
                      "justification": "O relato evidencia a desconstrução ativa de estereótipos negativos do balé ('sacrifício', 'maltrata o corpo'), mostrando que a metodologia da professora oferece uma 'outra visão' da dança."
                    },
                    {
                      "text": "Eu acho que a tua aula veio pra contribuir pra gente descobrir o limite do corpo e a possibilidade de fazer as coisas que tu achava muito impossível e tu podes fazer no teu limite",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "1",
                      "line": "25",
                      "found_indices": ["201"],
                      "indicator": "18",
                      "justification": "A fala destaca a descoberta e o respeito ao 'limite do corpo' como um elemento central da aula, característica de uma metodologia focada no cuidado e na prática consciente."
                    },
                    {
                      "text": "Ah eu acho que eu desconstruí a ideia que eu tinha do Balé, principalmente! A ideia que eu tinha da coisa chata, da coisa sacrificante, da coisa... assim...",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "1",
                      "line": "47",
                      "found_indices": ["202"],
                      "indicator": "19",
                      "justification": "A unidade expressa diretamente a 'desconstrução da ideia' de que o balé é algo 'chato' e 'sacrificante', um dos pilares da metodologia de ensino aplicada."
                    },
                    {
                      "text": "e eu acho que tu simplificava a coisa... e isso que facilitava e que era lá: tá pode ser agradável, pode ser prazeroso, né?",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "2",
                      "line": "14",
                      "found_indices": ["202", "201"],
                      "indicator": "19",
                      "justification": "Ao contrastar a prática com a ideia de sacrifício, associando-a ao prazer ('agradável', 'prazeroso'), a professora desconstrói um estereótipo central do balé."
                    },
                    {
                      "text": "depois (risos) que eu fiz Balé com a Mônica eu vi que não precisa ser sacrificante, não precisa ser isso...",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "2",
                      "line": "16",
                      "found_indices": ["202", "201"],
                      "indicator": "19",
                      "justification": "A fala do aluno confirma o sucesso da metodologia em desconstruir o estereótipo do balé como uma prática 'sacrificante', promovendo uma visão centrada no cuidado."
                    },
                    {
                      "text": "(risos) de amizade!!! (risos) né? É o que era mais legal das aulas é que a gente se divertia.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "2",
                      "line": "22",
                      "found_indices": ["203"],
                      "indicator": "20",
                      "justification": "A unidade descreve o ambiente de aprendizagem como sendo de 'amizade' e 'divertimento', características centrais de um clima de aula afetivo e lúdico."
                    },
                    {
                      "text": "se tu não divertir fazendo alguma coisa, se tu não rir, não, não, te ãaa, desconstruir aquela coisa de que sempre tem que ser perfeito, como eu ria muito dos meus erros e eu adoro os meus erros (risos)...",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "2",
                      "line": "23",
                      "found_indices": ["203"],
                      "indicator": "20",
                      "justification": "O ato de 'rir dos erros' e a valorização do divertimento são estratégias pedagógicas que definem um ambiente de aprendizagem afetivo, onde o erro é tratado com leveza."
                    },
                    {
                      "text": "(risos) era um divertimento né? (risos) não tem outra explicação.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "2",
                      "line": "30",
                      "found_indices": ["203"],
                      "indicator": "20",
                      "justification": "A caracterização da experiência do erro como 'um divertimento' reforça a presença de um ambiente de aprendizagem lúdico e positivo."
                    },
                    {
                      "text": "mas eu me lembro sempre daquela ideia da gente brincar assim, sempre teve aquela ideia também, eu acho que, não sei se por tu ser pedagoga, assim, né, então por exemplo, \"vamos fazer\" um... centro né, e ai tinha uma coisa mais assim, a gente brincava com aquilo",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "3",
                      "line": "38",
                      "found_indices": ["203", "199"],
                      "indicator": "20",
                      "justification": "A memória da 'ideia da gente brincar' e do ato de 'brincar com aquilo' (o exercício) demonstra a aplicação de uma metodologia que utiliza o lúdico como ferramenta pedagógica."
                    },
                    {
                      "text": "De afetividade, ponto! Não tem outra (risos)... acho que deve ser isto em qualquer ambiente de aprendizagem, é o principal...",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "4",
                      "line": "1",
                      "found_indices": ["203"],
                      "indicator": "20",
                      "justification": "A unidade elege a 'afetividade' como o elemento 'principal' do ambiente de aprendizagem, definindo o clima da aula como fundamentalmente afetivo."
                    },
                    {
                      "text": "a gente se divertia muito é a única coisa que eu me lembro assim, tirando a minha tensão de estar fazendo uma aula de Balé, eu me lembro muito disso assim, da gente rir muito, se divertir muito, rir muito dos erros. E tu brinca muito com a gente (risos)",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "4",
                      "line": "10",
                      "found_indices": ["203"],
                      "indicator": "20",
                      "justification": "O relato reitera a presença constante de elementos lúdicos ('se divertia', 'rir muito dos erros', 'brinca muito') que caracterizam o ambiente de aprendizagem."
                    },
                    {
                      "text": "o que me faltava é o que o Balé dá: é a consciência corporal que o Balé dá, ã, a noção cinesiológica que o Balé dá, anatômica que o Balé dá, fisiológica que o Balé dá, e tudo de proteção da articulação e de musculatura tudo",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "7",
                      "line": "24",
                      "found_indices": ["201"],
                      "indicator": "18",
                      "justification": "A unidade descreve o aprendizado como a aquisição de 'consciência corporal' e 'proteção', elementos de uma metodologia que prioriza o cuidado e o respeito ao corpo."
                    },
                    {
                      "text": "tem que fazer um caminho certo que não vai te machucar, e o Balé o que faz? É te proteger, ele te protege",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "7",
                      "line": "34",
                      "found_indices": ["201"],
                      "indicator": "18",
                      "justification": "A percepção de que o balé, conforme ensinado, 'protege' o corpo de lesões é um forte indicador de uma metodologia centrada no cuidado e na saúde corporal."
                    },
                    {
                      "text": "tu pedia pro pessoal sempre fazer aquele registro, o memorial no final da aula, pedindo pra pensar em como adaptar aquilo também, como é que tava sentindo...",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "7",
                      "line": "48",
                      "found_indices": ["205"],
                      "indicator": "22",
                      "justification": "A menção ao 'memorial no final da aula' como uma prática regular evidencia o estímulo à reflexão sobre o próprio processo de aprendizagem, uma característica da metodologia."
                    },
                    {
                      "text": "Então ã, tu respeitavas o, o tempo de desenvolvimento de cada um. E isso é uma coisa boa também, porque dentro de uma turma tu pode ter alguém super avançado... e alguém que nunca fez nada, como eu!",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "8",
                      "line": "9",
                      "found_indices": ["201"],
                      "indicator": "18",
                      "justification": "O respeito ao 'tempo de desenvolvimento de cada um' é uma prática pedagógica que demonstra o cuidado e o respeito aos limites individuais dos alunos."
                    },
                    {
                      "text": "o Balé fortalece de tal modo a minha musculatura que, que me ajudava no dia-a-dia.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "9",
                      "line": "6",
                      "found_indices": ["201"],
                      "indicator": "18",
                      "justification": "O relato de um benefício físico concreto (fortalecimento) que auxilia na vida cotidiana demonstra um resultado da metodologia focada no cuidado e na saúde corporal."
                    },
                    {
                      "text": "daí fortaleceu e eu não tive absolutamente nada! Ãaa, me protegeu as minhas articulações e a musculatura... e me ajudou, num problema de saúde.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "9",
                      "line": "13",
                      "found_indices": ["201"],
                      "indicator": "18",
                      "justification": "A experiência de que a prática do balé 'protegeu' o corpo de uma lesão mais grave reforça a percepção de uma metodologia que promove um corpo saudável e resiliente."
                    },
                    {
                      "text": "o erro era tratado com naturalidade, porque ele faz parte né",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "9",
                      "line": "18",
                      "found_indices": ["203"],
                      "indicator": "20",
                      "justification": "A forma como o erro é 'tratado com naturalidade' é um componente essencial da criação de um ambiente de aprendizagem afetivo e seguro para o aluno."
                    },
                    {
                      "text": "todo mundo lidava com o erro de maneira bem humorada e divertida, então não tinha problema nenhum, não aquele... ninguém ali ia morrer, se atirar pela janela porque não levantou a perna na cabeça, sabe?",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "9",
                      "line": "21",
                      "found_indices": ["203"],
                      "indicator": "20",
                      "justification": "A descrição do tratamento do erro de forma 'bem humorada e divertida' caracteriza um ambiente lúdico, que desconstrói a pressão e o perfeccionismo excessivo."
                    },
                    {
                      "text": "esses diários são bons por serem um momento de depois que tu faz uma aula depois que tu vivencia um processo desses tu vai pensar sobre aquilo ali, sobre o que aquela aula representou no teu corpo, sobre o que tu descobriu, o que tu vai refletir e tal",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "9",
                      "line": "40",
                      "found_indices": ["205"],
                      "indicator": "22",
                      "justification": "A unidade detalha a função dos 'diários de processo' como ferramenta para a reflexão sobre o aprendizado, estimulando uma prática consciente e não apenas executora."
                    },
                    {
                      "text": "aprender que as linhas não são só linhas no meu corpo porque fica bonito, mas é porque elas tem uma função e elas não limitam a gente, elas nosmi ajudam a ir além...",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "10",
                      "line": "19",
                      "found_indices": ["199", "201"],
                      "indicator": "18",
                      "justification": "A compreensão da 'função' do movimento para 'ir além' demonstra um aprendizado focado na consciência corporal e na superação de limites, e não apenas na estética, alinhando-se ao cuidado com o corpo."
                    },
                    {
                      "text": "Um conhecimento que protege! Um conhecimento que protege o corpo foi isso que o Balé me deu.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "10",
                      "line": "25",
                      "found_indices": ["201"],
                      "indicator": "18",
                      "justification": "A afirmação categórica de que o balé forneceu um 'conhecimento que protege o corpo' sintetiza o pilar da metodologia centrado no cuidado e na prevenção de lesões."
                    },
                    {
                      "text": "fazer sem compreender é que leva à lesão, leva machucado, prejudica...",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "11",
                      "line": "21",
                      "found_indices": ["199", "201"],
                      "indicator": "18",
                      "justification": "A conexão direta entre a falta de compreensão e o risco de lesão evidencia uma metodologia que integra o saber pedagógico ao cuidado com o corpo como forma de proteção."
                    },
                    {
                      "text": "tu conseguias trazer isso de uma forma que ãa, que vai ter que doer, mexer com a musculatura né, que isso não fosse tão dolorido, fosse agradável, divertido...",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "11",
                      "line": "39",
                      "found_indices": ["204", "202", "203"],
                      "indicator": "19",
                      "justification": "A capacidade da professora de transformar uma experiência potencialmente 'dolorida' em algo 'agradável, divertido' demonstra a aplicação de uma metodologia que desconstrói estereótipos e utiliza o lúdico."
                    },
                    {
                      "text": "a gente também fazia no final um teatrinho que a gente tinha que fazer passos de balé e essas coisas.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "32",
                      "line": "11",
                      "found_indices": ["203", "205"],
                      "indicator": "20",
                      "justification": "A prática do 'teatrinho' no final da aula é um exemplo claro de uma estratégia que combina o lúdico com a prática criativa, caracterizando o ambiente de aprendizagem."
                    },
                    {
                      "text": "tinha momento que a gente parava pra conversar, tinha os momentos que a gente fazia essas brincadeiras que eu tinha falado antes, tinha vários momentos legais e descontraídos.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "32",
                      "line": "16",
                      "found_indices": ["203"],
                      "indicator": "20",
                      "justification": "A menção a 'brincadeiras' e 'momentos legais e descontraídos' descreve um ambiente de aula afetivo e lúdico, que vai além da instrução técnica."
                    },
                    {
                      "text": "eu lembro de uma vez que tu fez umas comidinhas e aí a gente tinha que imitar ser uma formiga e a gente tina que ir pegando as comidinhas pra chegar de baixo do tnt que era a toca das formigas.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "32",
                      "line": "25",
                      "found_indices": ["203"],
                      "indicator": "20",
                      "justification": "O relato de uma atividade lúdica e imaginativa ('imitar ser uma formiga') exemplifica a metodologia que utiliza o 'teatrinho' e a brincadeira para engajar os alunos."
                    },
                    {
                      "text": "O teatrinho... tu dava um tema pra gente e a gente tinha que fazer duplas ou trios pra gente poder fazer o teatrinho e tinha que ter pelo menos uns cinco (5) passos de balé pra ser válido.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "32",
                      "line": "30",
                      "found_indices": ["205"],
                      "indicator": "22",
                      "justification": "A descrição do 'teatrinho' como uma atividade com tema e regras (incluir passos de balé) mostra como a professora estimula a prática criativa de forma estruturada."
                    },
                    {
                      "text": "Porque no teatrinho existia uma liberdade de expressão, né? Então a gente podia fazer o que a gente quisesse se enquadrando nos temas. Então eu acho que isso é bom porque estimula a criatividade da pessoa.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "33",
                      "line": "10",
                      "found_indices": ["205"],
                      "indicator": "22",
                      "justification": "A unidade define o 'teatrinho' como um espaço de 'liberdade de expressão' que 'estimula a criatividade', alinhando-se perfeitamente à categoria de estímulo à prática criativa."
                    },
                    {
                      "text": "Tu tava sempre estimulando a gente pra, nas criações, na criatividade... tu sempre procurou estimular o nosso lado criativo, o nosso lado da imaginação, né? O nosso lado criança mesmo.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "33",
                      "line": "24",
                      "found_indices": ["205"],
                      "indicator": "22",
                      "justification": "O aluno percebe e verbaliza a intenção da professora de 'estimular o lado criativo' e a 'imaginação', confirmando esta como uma característica central de sua metodologia."
                    },
                    {
                      "text": "tu não vai encontra outro curso onde a professora brinca e conversa direito contigo. Tu não vai encontrar outro curso em que tu pode... ãaaa, sei lá, criar coisas na aula.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "33",
                      "line": "29",
                      "found_indices": ["203", "205"],
                      "indicator": "20",
                      "justification": "A unidade destaca o ato de 'brincar', 'conversar' e 'criar coisas na aula' como diferenciais da metodologia, apontando para um ambiente lúdico e que estimula a criatividade."
                    },
                    {
                      "text": "eu aprendia bastante com as brincadeiras e também nos momentos sérios, quando a gente era meio na marra e tinha que aprender, mas era isso, entre os momentos de brincadeiras e os momentos sérios, a gente acabava aprendendo bastante coisa.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "33",
                      "line": "39",
                      "found_indices": ["203"],
                      "indicator": "20",
                      "justification": "A fala confirma que as 'brincadeiras' são uma parte efetiva do processo de aprendizagem, validando o lúdico como uma ferramenta pedagógica na metodologia da professora."
                    },
                    {
                      "text": "A professora sempre foi a coisa mais calma do mundo, né? Ela ia lá, corrigia o erro, falava o que tinha que fazer com a maior calma do mundo e aí se a gente errava de ovo ela ia á e explicava de novo, com a mesma calma de sempre",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "35",
                      "line": "7",
                      "found_indices": ["203"],
                      "indicator": "20",
                      "justification": "A descrição da maneira calma e paciente com que a professora lida com o erro ('corrigia o erro com a maior calma') caracteriza um ambiente de aprendizagem afetivo e seguro."
                    },
                    {
                      "text": "eu percebi que o balé não precisa ser uma coisa tão séria quanto ele parece ser. O balé pode ser uma coisa feliz, ele não po... ele não precisa ser uma coisa monótona, sempre naquele jeito, naquele estilo. Então eu acho que esse jeito teu de ensinar, esse jeito teu de brincar, me estimula a continuar dançando balé até hoje.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "35",
                      "line": "21",
                      "found_indices": ["202", "203"],
                      "indicator": "19",
                      "justification": "A unidade mostra a desconstrução do estereótipo do balé 'sério' e 'monótono', associando a metodologia ('jeito de brincar') a uma experiência 'feliz' que estimula o aluno."
                    },
                    {
                      "text": "porque todos os professores de balé sempre fazem aquela coisa séria, que vão te falar os passos que tu vai ter que fazer e não te dão liberdade nenhuma.",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "35",
                      "line": "29",
                      "found_indices": ["202", "205"],
                      "indicator": "19",
                      "justification": "Ao contrastar com a 'coisa séria' de outros professores que 'não dão liberdade', o trecho evidencia que a metodologia da professora se diferencia por desconstruir essa rigidez e estimular a autonomia."
                    },
                    {
                      "text": "eu lembro que a gente até opinava nas apresentações de final de ano, não era nem a professora que decidia, era a gente, então a gente tinha bastante liberdade pra ser o que a gente quisesse ser",
                      "document": "EntrevistasExemplo.pdf",
                      "page": "35",
                      "line": "35",
                      "found_indices": ["205"],
                      "indicator": "22",
                      "justification": "A prática de permitir que os alunos 'opinassem' e decidissem sobre as apresentações é um forte indicador do estímulo à autonomia, à expressão e à tomada de decisão, componentes da prática criativa."
                    }
                  ]
                }
              ]
            }
            ```
            """;
    }
}

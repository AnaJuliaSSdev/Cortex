using Cortex.Data;
using Cortex.Models;
using Cortex.Repositories;
using Cortex.Repositories.Interfaces;
using Cortex.Services;
using Cortex.Services.Interfaces;
using Cortex.Services.ServicosTopicos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text; // Adicionado para o StringBuilder

namespace ChatIAComRAG;

class Program
{
    static async Task Main(string[] args)
    {
        // 1. Verificar se o modo debug foi solicitado
        // Ex: dotnet run "caminho/do/pdf.pdf" showInfo
        bool isDebugMode = args.Any(arg => arg.Equals("showInfo", StringComparison.OrdinalIgnoreCase));

        // 2. Tentar encontrar o caminho do PDF nos argumentos
        // Ele ignora o argumento "showInfo" e pega o primeiro argumento restante
        string? pdfPath = args.FirstOrDefault(arg => !arg.Equals("showInfo", StringComparison.OrdinalIgnoreCase));

        // Configurar serviços (passando o modo de debug)
        var services = ConfigureServices(isDebugMode);
        var serviceProvider = services.BuildServiceProvider();

        // Header da Aplicação
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine("      SISTEMA DE RAG - Chat com PDF usando Gemini");
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine();
        Console.ResetColor();

        // Obter serviços
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        //var pdfProcessor = serviceProvider.GetRequiredService<SimplePdfProcessingService>();
        var ragService = serviceProvider.GetRequiredService<IRagService>();

        try
        {
            // Se nenhum caminho foi passado via argumento, pedir ao usuário
            if (string.IsNullOrWhiteSpace(pdfPath))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Digite o caminho completo do arquivo PDF: ");
                Console.ResetColor();
                pdfPath = Console.ReadLine();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Arquivo recebido por argumento: {pdfPath}");
                Console.ResetColor();
            }

            // Validar o caminho do arquivo
            if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Arquivo não encontrado!");
                Console.ResetColor();
                return;
            }

            var title = Path.GetFileNameWithoutExtension(pdfPath);

            Console.WriteLine();
            Console.WriteLine($"Processando '{title}' (pode levar alguns minutos)...");
            Console.WriteLine();

            // Processar o PDF
            //var document = await pdfProcessor.ProcessPdfAsync(pdfPath, title);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("PDF processado com sucesso");
            Console.ResetColor();
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine("   Você já pode fazer perguntas sobre o documento!");
            Console.WriteLine("   Digite 'sair' para encerrar");
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine();
            Console.ResetColor();

            var chatHistory = new List<(string question, string answer)>();

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("Você: ");
                Console.ResetColor();

                var question = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(question) ||
                    question.Equals("sair", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine();
                    Console.WriteLine("Encerrando chat.");
                    break;
                }

                // Montar histórico
                var promptBuilder = new StringBuilder();
                if (chatHistory.Any())
                {
                    promptBuilder.AppendLine("Histórico da conversa anterior:");
                    // Pega as últimas 2 interações
                    foreach (var (q, a) in chatHistory.TakeLast(2))
                    {
                        promptBuilder.AppendLine($"Você: {q}");
                        promptBuilder.AppendLine($"Assistente: {a}\n");
                    }
                    promptBuilder.AppendLine("---");
                }
                promptBuilder.AppendLine($"Pergunta atual: {question}");

                var contextualQuestion = promptBuilder.ToString();

                // Limpar a linha e mostrar status
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⏳ Buscando resposta...");
                Console.ResetColor();

                //var answer = await ragService.AskQuestionAsync(contextualQuestion, document.Id);

                // Mostrar a resposta
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Assistente:");
                Console.ResetColor();
                //Console.WriteLine(answer);
                Console.WriteLine();
                Console.WriteLine("-".PadRight(60, '-'));
                Console.WriteLine();

                // Salvar no histórico
                //chatHistory.Add((question, answer));
            }

            // Mostrar estatísticas
            if (chatHistory.Any())
            {
                Console.WriteLine();
                Console.WriteLine("📊 Estatísticas da sessão:");
                Console.WriteLine($"   - Total de perguntas: {chatHistory.Count}");
                Console.WriteLine($"   - Documento: {title}");
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Erro: {ex.Message}");
            logger.LogCritical(ex, "Ocorreu um erro fatal na aplicação.");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Configura os serviços de DI
    /// </summary>
    /// <param name="isDebugMode">Flag para ligar ou desligar os logs</param>
    static IServiceCollection ConfigureServices(bool isDebugMode)
    {
        var services = new ServiceCollection();

        // Configuração
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets<Program>()
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        services.AddLogging(builder =>
        {
            builder.ClearProviders();

            LogLevel minLevel = isDebugMode ? LogLevel.Information : LogLevel.None;
            builder.SetMinimumLevel(minLevel);

            builder.AddConsole();

            if (isDebugMode)
            {
                builder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            }
        });

        Npgsql.NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();

        // DbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                o => o.UseVector()
            )
        );

        // Configurações
        services.Configure<GeminiConfiguration>(configuration.GetSection(GeminiConfiguration.SectionName));

        // HttpClient
        services.AddHttpClient<IEmbeddingService, EmbeddingService>();

        // Repositórios
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IChunkRepository, ChunkRepository>();

        // Serviços
        services.AddScoped<IChunkService, ChunkService>();
        services.AddScoped<IEmbeddingService, EmbeddingService>();
        services.AddScoped<IGeminiService, GeminiService.Api.Services.Implementations.GeminiService>();
        services.AddScoped<IRagService, RagService>();
        //services.AddScoped<SimplePdfProcessingService>();

        return services;
    }
}
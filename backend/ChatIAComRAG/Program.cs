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

namespace ChatIAComRAG;

class Program
{
    static async Task Main(string[] args)
    {
        // Configurar serviços
        var services = ConfigureServices();
        var serviceProvider = services.BuildServiceProvider();

        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine("  SISTEMA DE RAG - Chat com PDF usando Gemini");
        Console.WriteLine("=".PadRight(60, '='));
        Console.WriteLine();

        var pdfProcessor = serviceProvider.GetRequiredService<SimplePdfProcessingService>();
        var ragService = serviceProvider.GetRequiredService<IRagService>();

        // Passo 1: Upload do PDF
        Console.Write("Digite o caminho completo do arquivo PDF: ");
        var pdfPath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
        {
            Console.WriteLine("Arquivo não encontrado!");
            return;
        }

        Console.Write("Digite um título para o documento: ");
        var title = Console.ReadLine() ?? "Documento sem título";

        Console.WriteLine();
        Console.WriteLine("Processando PDF (pode levar alguns minutos)...");
        Console.WriteLine();

        try
        {
            var document = await pdfProcessor.ProcessPdfAsync(pdfPath, title);

            Console.WriteLine("PDF processado com sucesso");
            Console.WriteLine($"   - ID do documento: {document.Id}");
            Console.WriteLine($"   - Total de caracteres: {document.Content?.Length ?? 0}");
            Console.WriteLine();

            // Passo 2: Chat
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine("  Você já pode fazer perguntas sobre o documento!");
            Console.WriteLine("  Digite 'sair' para encerrar");
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine();

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

                // Adicionar histórico ao contexto (opcional)
                var contextualQuestion = question;
                if (chatHistory.Any())
                {
                    var lastInteraction = chatHistory.Last();
                    contextualQuestion = $"Contexto da última pergunta:\nPergunta: {lastInteraction.question}\nResposta: {lastInteraction.answer}\n\nNova pergunta: {question}";
                }

                Console.WriteLine();
                Console.WriteLine("⏳ Buscando resposta...");
                Console.WriteLine();

                var answer = await ragService.AskQuestionAsync(contextualQuestion, document.Id);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Assistente:");
                Console.ResetColor();
                Console.WriteLine(answer);
                Console.WriteLine();
                Console.WriteLine("-".PadRight(60, '-'));
                Console.WriteLine();

                // Salvar no histórico
                chatHistory.Add((question, answer));
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
            Console.ResetColor();
        }
    }

    static IServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();

        // Configuração
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddUserSecrets<Program>()
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // ⚠️ IMPORTANTE: EnableDynamicJson ANTES do DbContext
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
        services.AddScoped<SimplePdfProcessingService>();

        return services;
    }
}
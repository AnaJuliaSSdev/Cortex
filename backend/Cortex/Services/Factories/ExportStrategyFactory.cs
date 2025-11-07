using Cortex.Models;
using Cortex.Services.Interfaces;

namespace Cortex.Services.Factories;

public class ExportStrategyFactory
{
    // Usamos um dicionário para registrar as estratégias
    // Isso é mais escalável que um switch-case
    private readonly IServiceProvider _serviceProvider;

    public ExportStrategyFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        // No Program.cs, você registrará as estratégias, ex:
        // services.AddScoped<PdfExportStrategy>();
        // services.AddScoped<LatexExportStrategy>();
    }

    public IExportStrategy GetStrategy(string format)
    {
        // Normaliza o formato para minúsculas
        string normalizedFormat = format.ToLowerInvariant();

        Type strategyType = normalizedFormat switch
        {
            "pdf" => typeof(PdfExportStrategy),
            "latex" or "tex" => typeof(LatexExportStrategy),
            // "csv" => typeof(CsvExportStrategy), // Exemplo futuro
            // "word" or "docx" => typeof(WordExportStrategy), // Exemplo futuro
            _ => throw new NotSupportedException($"O formato de exportação '{format}' não é suportado.")
        };

        // Usa o Service Provider para obter a instância da estratégia
        // Isso permite que as estratégias (como PdfExportStrategy)
        // tenham suas próprias dependências (como ILogger) injetadas.
        var strategy = _serviceProvider.GetService(strategyType) as IExportStrategy;

        if (strategy == null)
            throw new InvalidOperationException($"Não foi possível resolver a estratégia para o formato '{format}'. Você registrou {strategyType.Name} no Program.cs?");

        return strategy;
    }
}

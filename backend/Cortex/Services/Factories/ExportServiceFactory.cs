using Cortex.Models;
using Cortex.Models.DTO;
using Cortex.Models.Enums;
using Cortex.Services.Interfaces;
using Cortex.Services.Strategies;

namespace Cortex.Services.Factories;

/// <summary>
/// Factory para criação de serviços de exportação.
/// Implementa o padrão Factory e utiliza DI do ASP.NET Core.
/// </summary>
public class ExportServiceFactory : IExportServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<ExportType, Type> _serviceMap;

    public ExportServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        // Mapeamento de tipos de exportação para implementações
        _serviceMap = new Dictionary<ExportType, Type>
        {
            { ExportType.PDF, typeof(PdfExportService) },
            // { ExportType.LaTeX, typeof(LatexExportService) }
            // { ExportType.Word, typeof(WordExportService) },
            // { ExportType.Excel, typeof(ExcelExportService) }
        };
    }

    public IExportService CreateExportService(ExportType type)
    {
        if (!_serviceMap.ContainsKey(type))
        {
            throw new NotSupportedException($"Tipo de exportação '{type}' não suportado");
        }

        var serviceType = _serviceMap[type];
        var service = _serviceProvider.GetService(serviceType) as IExportService;

        if (service == null)
        {
            throw new InvalidOperationException($"Não foi possível criar serviço de exportação para tipo '{type}'");
        }

        return service;
    }

    public IEnumerable<ExportType> GetSupportedTypes()
    {
        return _serviceMap.Keys;
    }
}

/// <summary>
/// Implementação simples do formatador de dados.
/// Pode ser expandida para diferentes estilos de formatação.
/// </summary>
public class AnalysisDataFormatter : IAnalysisDataFormatter
{
    public string FormatCategoryName(string name)
    {
        return name?.Trim() ?? "Sem nome";
    }

    public string FormatFrequency(int frequency)
    {
        return frequency == 1
            ? "1 ocorrência"
            : $"{frequency} ocorrências";
    }

    public string FormatIndexName(string name)
    {
        return name?.Trim() ?? "Índice sem nome";
    }

    public string FormatReference(string document, string page)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(document))
            parts.Add($"Doc: {document}");

        if (!string.IsNullOrEmpty(page))
            parts.Add($"p. {page}");

        return parts.Any() ? string.Join(", ", parts) : "Sem referência";
    }

    public TableData CreateCategorySummaryTable(List<Models.Category> categories)
    {
        return new TableData
        {
            Caption = "Resumo das categorias identificadas",
            Headers = new List<string> { "Categoria", "Frequência", "Definição" },
            Rows = categories.Select(c => new List<string>
            {
                FormatCategoryName(c.Name),
                FormatFrequency(c.Frequency),
                c.Definition ?? "-"
            }).ToList()
        };
    }

    public TableData CreateIndexDetailTable(Models.Index index)
    {
        var table = new TableData
        {
            Caption = $"Detalhes do índice: {index.Name}",
            Headers = new List<string> { "Propriedade", "Valor" },
            Rows = new List<List<string>>
            {
                new() { "Nome", FormatIndexName(index.Name) },
                new() { "Descrição", index.Description ?? "-" },
                new() { "Indicador", index.Indicator?.Name ?? "-" }
            }
        };

        return table;
    }
}

/// <summary>
/// Classe de extensão para configurar os serviços de exportação no DI container
/// </summary>
public static class ExportServiceExtensions
{
    public static IServiceCollection AddExportServices(this IServiceCollection services)
    {
        // Registrar serviços base
        services.AddScoped<IAnalysisDataFormatter, AnalysisDataFormatter>();
        services.AddScoped<IExportServiceFactory, ExportServiceFactory>();

        // Registrar serviços de exportação específicos
        services.AddScoped<PdfExportService>();
        // services.AddScoped<LatexExportService>();
        // services.AddScoped<WordExportService>();
        // services.AddScoped<ExcelExportService>();

        return services;
    }
}
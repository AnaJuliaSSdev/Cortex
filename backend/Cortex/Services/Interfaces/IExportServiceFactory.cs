using Cortex.Models.Enums;

namespace Cortex.Services.Interfaces;

public interface IExportServiceFactory
{
    IExportService CreateExportService(ExportType type);
    IEnumerable<ExportType> GetSupportedTypes();
}

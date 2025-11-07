using Cortex.Models;
using Cortex.Models.DTO;
using Index = Cortex.Models.Index;

namespace Cortex.Services.Interfaces;

public interface IAnalysisDataFormatter
{
    string FormatCategoryName(string name);
    string FormatFrequency(int frequency);
    string FormatIndexName(string name);
    string FormatReference(string document, string page, string line);
    TableData CreateCategorySummaryTable(List<Category> categories);
    TableData CreateIndexDetailTable(Index index);
}

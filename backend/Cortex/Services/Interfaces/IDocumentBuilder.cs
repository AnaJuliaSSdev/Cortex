using Cortex.Models.DTO;

namespace Cortex.Services.Interfaces;

public interface IDocumentBuilder
{
    void AddTitle(string title);
    void AddSection(string sectionTitle);
    void AddParagraph(string text);
    void AddTable(TableData tableData);
    void AddImage(byte[] imageData, string caption);
    void AddPageBreak();
    byte[] Build();
}

// using Cortex.Models.DTO;
// using Cortex.Services.Interfaces;
// using System.Globalization;
// using System.Text;

// namespace Cortex.Services;

// public class LatexDocumentBuilder : IDocumentBuilder
// {
//     private readonly StringBuilder _sb;
//     private readonly ILogger<LatexDocumentBuilder> _logger;

//     public LatexDocumentBuilder(ILogger<LatexDocumentBuilder> logger)
//     {
//         _logger = logger;
//         _sb = new StringBuilder();

//         // Preâmbulo padrão do LaTeX (pacotes comuns)
//         _sb.AppendLine(@"\documentclass[12pt, a4paper]{article}");
//         _sb.AppendLine(@"\usepackage[utf8]{inputenc}");
//         _sb.AppendLine(@"\usepackage[T1]{fontenc}");
//         _sb.AppendLine(@"\usepackage{graphicx}"); // Para imagens
//         _sb.AppendLine(@"\usepackage{booktabs}"); // Para tabelas bonitas
//         _sb.AppendLine(@"\usepackage{longtable}"); // Para tabelas que quebram páginas
//         _sb.AppendLine(@"\usepackage[margin=2.5cm]{geometry}"); // Margens (similar ao ABNT)
//         _sb.AppendLine(@"\usepackage{hyperref}"); // Para links (se houver)
//         _sb.AppendLine(@"\hypersetup{colorlinks=true, linkcolor=blue, urlcolor=blue}");
//         _sb.AppendLine(@"\title{}"); // Será preenchido por AddTitle
//         _sb.AppendLine(@"\author{Sistema Cortex}");
//         _sb.AppendLine(@"\date{\today}");
//         _sb.AppendLine(@"\begin{document}");
//         _sb.AppendLine(@"\maketitle");
//     }

//     // Escapa caracteres especiais do LaTeX
//     private string Escape(string? text)
//     {
//         if (string.IsNullOrEmpty(text)) return string.Empty;
//         return text.Replace(@"\", @"\textbackslash{}")
//                    .Replace("{", @"\{")
//                    .Replace("}", @"\}")
//                    .Replace("_", @"\_")
//                    .Replace("^", @"\^{}")
//                    .Replace("&", @"\&")
//                    .Replace("%", @"\%")
//                    .Replace("$", @"\$")
//                    .Replace("#", @"\#")
//                    .Replace("~", @"\textasciitilde{}");
//     }

//     public void AddTitle(string title)
//     {
//         // Substitui o \title{} vazio no preâmbulo
//         _sb.Replace(@"\title{}", $@"\title{{{Escape(title)}}}");
//     }

//     public void AddSection(string sectionTitle)
//     {
//         _sb.AppendLine($@"\section*{{{Escape(sectionTitle)}}}"); // Usa '*' para não numerar
//     }

//     public void AddParagraph(string text)
//     {
//         _sb.AppendLine(Escape(text) + @"\par");
//     }

//     public void AddTable(TableData tableData)
//     {
//         if (tableData == null || tableData.Rows.Count == 0) return;

//         // O \subsection* está funcionando como seu título, vamos mantê-lo.
//         _sb.AppendLine($@"\subsection*{{{Escape(tableData.Caption)}}}");

//         string colFormat = "l";
//         if (tableData.Headers.Count > 1)
//         {
//             float colWidth = (0.8f / (tableData.Headers.Count - 1));

//             string colWidthStr = colWidth.ToString(CultureInfo.InvariantCulture);

//             for (int i = 1; i < tableData.Headers.Count; i++)
//             {
//                 // Use a string formatada correta
//                 colFormat += $@"p{{{colWidthStr}\textwidth}}";
//             }
//         }

//         _sb.AppendLine($@"\begin{{longtable}}{{@{{}}{colFormat}@{{}}}}");
//         _sb.AppendLine(@"\toprule");
//         _sb.AppendLine(string.Join(" & ", tableData.Headers.Select(h => $@"\textbf{{{Escape(h)}}}")) + @" \\");
//         _sb.AppendLine(@"\midrule");
//         _sb.AppendLine(@"\endfirsthead");
//         _sb.AppendLine(@"\toprule");
//         _sb.AppendLine(string.Join(" & ", tableData.Headers.Select(h => $@"\textbf{{{Escape(h)}}}")) + @" \\");
//         _sb.AppendLine(@"\midrule");
//         _sb.AppendLine(@"\endhead");
//         _sb.AppendLine(@"\bottomrule");
//         _sb.AppendLine(@"\endfoot");
//         _sb.AppendLine(@"\bottomrule");
//         _sb.AppendLine(@"\endlastfoot");
//         foreach (var row in tableData.Rows)
//         {
//             _sb.AppendLine(string.Join(" & ", row.Select(c => Escape(c ?? "-"))) + @" \\");
//         }
//         _sb.AppendLine(@"\end{{longtable}}");
//     }

//     public void AddImage(byte[] imageData, string caption)
//     {
//         // AVISO: LaTeX não pode embutir bytes de imagem diretamente em um .tex.
//         // Ele precisa de um arquivo (ex: .png) no mesmo diretório.
//         // Como a arquitetura atual retorna um SÓ arquivo, não podemos
//         // incluir a imagem. Vamos adicionar um placeholder.
//         string imageName = $"grafico_{Guid.NewGuid().ToString().Substring(0, 8)}.png";

//         _logger.LogWarning(
//             "Exportação LaTeX: A imagem (gráfico) não pode ser embutida em um arquivo .tex. " +
//             "Um placeholder foi adicionado. Para incluir a imagem, o arquivo '{imageName}' " +
//             "precisa ser salvo no mesmo diretório do .tex e o documento compilado (ex: com pdfLaTeX).", imageName);

//         _sb.AppendLine(@"\begin{figure}[h!]");
//         _sb.AppendLine(@"\centering");
//         _sb.AppendLine($@"% {Escape(caption)}");
//         _sb.AppendLine($@"% SUBSTITUA A LINHA ABAIXO PELA SUA IMAGEM:");
//         _sb.AppendLine($@"\framebox[0.8\textwidth]{{\parbox[c][10cm][c]{{0.75\textwidth}}{{\centering \textbf{{PLACEHOLDER DA IMAGEM}} \par {Escape(caption)} \par ({Escape(imageName)})}}}}");
//         // Linha de código real se a imagem existisse:
//         // _sb.AppendLine($@"\includegraphics[width=0.8\textwidth]{{{imageName}}}");
//         _sb.AppendLine($@"\caption{{{Escape(caption)}}}");
//         _sb.AppendLine(@"\end{figure}");
//     }

//     public void AddPageBreak()
//     {
//         _sb.AppendLine(@"\newpage");
//     }

//     public byte[] Build()
//     {
//         _sb.AppendLine(@"\end{document}");
//         return Encoding.UTF8.GetBytes(_sb.ToString());
//     }
// }

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace TD_KT.Services
{
    /// <summary>
    /// Xuất báo cáo ra .docx bằng OpenXML SDK (DocumentFormat.OpenXml).
    /// </summary>
    public class WordDocxExportService
    {
        /// <summary>
        /// Xuất 1 bảng bất kỳ ra .docx (dạng bảng giống Tab 2 báo cáo khen thưởng).
        /// tableRows: dòng đầu tiên nên là header.
        /// </summary>
        public void ExportTableReport(string filePath, string title, IEnumerable<string> filterLines, IEnumerable<IList<string>> tableRows)
        {
            // Reuse logic hiện có để đảm bảo file mở/sửa bình thường.
            ExportRewardReport(filePath, title, filterLines, tableRows);
        }

        public void ExportRewardReport(string filePath, string title, IEnumerable<string> filterLines, IEnumerable<IList<string>> tableRows)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using (var doc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document(new Body());

                var body = mainPart.Document.Body;

                // Title
                body.Append(CreateParagraph(title, isBold: true, fontSize: 28, alignment: JustificationValues.Center));
                body.Append(CreateParagraph(string.Empty));

                if (filterLines != null)
                {
                    foreach (var line in filterLines.Where(x => !string.IsNullOrWhiteSpace(x)))
                        body.Append(CreateParagraph(line, isBold: false, fontSize: 22));
                    body.Append(CreateParagraph(string.Empty));
                }

                // Table
                var table = new Table();

                var tableProps = new TableProperties(
                    new TableBorders(
                        new TopBorder { Val = BorderValues.Single, Size = 8 },
                        new BottomBorder { Val = BorderValues.Single, Size = 8 },
                        new LeftBorder { Val = BorderValues.Single, Size = 8 },
                        new RightBorder { Val = BorderValues.Single, Size = 8 },
                        new InsideHorizontalBorder { Val = BorderValues.Single, Size = 8 },
                        new InsideVerticalBorder { Val = BorderValues.Single, Size = 8 }
                    )
                );
                table.AppendChild(tableProps);

                foreach (var row in tableRows)
                {
                    var tr = new TableRow();
                    foreach (var cellText in row)
                    {
                        var tc = new TableCell();
                        tc.Append(new Paragraph(new Run(new Text(cellText ?? string.Empty) { Space = SpaceProcessingModeValues.Preserve })));
                        tc.Append(new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Auto }));
                        tr.Append(tc);
                    }
                    table.Append(tr);
                }

                body.Append(table);

                mainPart.Document.Save();
            }
        }

        private static Paragraph CreateParagraph(string text, bool isBold = false, int fontSize = 22)
        => CreateParagraph(text, isBold, fontSize, JustificationValues.Left);

        private static Paragraph CreateParagraph(string text, bool isBold, int fontSize, JustificationValues alignment)
        {
            var runProps = new RunProperties();
            if (isBold) runProps.Append(new Bold());
            runProps.Append(new FontSize { Val = fontSize.ToString() });

            var run = new Run(runProps, new Text(text ?? string.Empty) { Space = SpaceProcessingModeValues.Preserve });

            var paraProps = new ParagraphProperties(new Justification { Val = alignment });
            return new Paragraph(paraProps, run);
        }

    }
}

using Application.Contracts.Services;
using ClosedXML.Excel;
using Respira.ServiceDefaults.Exceptions;

namespace Infrastructure.Services.Files
{
    public class ExportService : IExportService
    {
        private static string GetCell(int column, int row)
        {
            if (column < 1 || row < 1)
            {
                throw new UnexpectedException("Column and row must be greater than 0");
            }

            // For simple and platform compatibility, we only support
            // 26 columns and rows up to 1e4
            if (column > 26 || row > 1e4)
            {
                throw new UnexpectedException("Worksheet only support 26 columns and up to 10.000 rows");
            }

            return $"{(char)('A' + column - 1)}{row}";
        }

        public async Task<byte[]> Export(List<Column> columns, HeaderStyleOption? headerStyle = null)
        {
            // Create a workbook
            var workbook = new XLWorkbook();

            // Add worksheet
            var worksheet = workbook.Worksheets.Add("Monthly report");

            // Create table header
            for (var i = 0; i < columns.Count; i++)
            {
                var column = columns[i];

                // Create header
                worksheet.Cell(GetCell(i + 1, 1)).Value = column.Header;

                // Create column value
                for (int j = 0; j < column.Values.Count; j++)
                {
                    worksheet.Cell(GetCell(i + 1, j + 2)).Value = column.Values[j];
                }
            }

            // Apply header style
            if (headerStyle is not null)
            {
                var header = worksheet.Range($"{GetCell(1, 1)}:{GetCell(columns.Count, 1)}");
                if (headerStyle.Align)
                {
                    header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }

                if (headerStyle.Bold)
                {
                    header.Style.Font.Bold = true;
                }

            }

            // Adjust column width based on content
            worksheet.Columns().AdjustToContents();

            await using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return stream.ToArray();
        }
    }
}

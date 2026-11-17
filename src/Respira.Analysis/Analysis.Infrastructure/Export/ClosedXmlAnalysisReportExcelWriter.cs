using ClosedXML.Excel;
using Respira.Analysis.Application.Contracts;
using Respira.Analysis.Application.Contracts.Export;

namespace Respira.Analysis.Infrastructure.Export;

/// <summary>Renders an analysis report as a styled multi-sheet Excel workbook.</summary>
public sealed class ClosedXmlAnalysisReportExcelWriter : IAnalysisReportExcelWriter
{
    private const string OverviewSheetName = "Tổng quan";
    private const string DoctorSheetName = "Theo bác sĩ";
    private const string MedicineSheetName = "Theo thuốc";
    private const string EmptyMessage = "Không có dữ liệu";

    private const string IntegerFormat = "#,##0";
    private const string PercentFormat = "0.0%";
    private const string DateTimeFormat = "dd/MM/yyyy HH:mm";

    private static readonly XLColor HeaderFill = XLColor.FromHtml("#1F4E78");
    private static readonly XLColor TitleColor = XLColor.FromHtml("#1F4E78");
    private static readonly XLColor TotalFill = XLColor.FromHtml("#D9E2F3");

    private static readonly TimeZoneInfo ReportTimeZone = ResolveReportTimeZone();

    public byte[] Write(AnalysisReportResult report)
    {
        using var workbook = new XLWorkbook();

        WriteOverviewSheet(workbook, report);
        WriteDoctorSheet(workbook, report);
        WriteMedicineSheet(workbook, report);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteOverviewSheet(XLWorkbook workbook, AnalysisReportResult report)
    {
        var sheet = workbook.Worksheets.Add(OverviewSheetName);

        sheet.Range(1, 1, 1, 4).Merge();
        var title = sheet.Cell(1, 1);
        title.Value = "BÁO CÁO PHÂN TÍCH ĐIỀU TRỊ";
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = 16;
        title.Style.Font.FontColor = TitleColor;
        title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        WriteMetadata(sheet, 2, "Mã báo cáo", report.AnalysisId.ToString());
        WriteMetadata(
            sheet,
            3,
            "Bác sĩ phụ trách",
            report.DoctorId?.ToString() ?? "Tất cả bác sĩ"
        );

        sheet.Cell(4, 1).Value = "Khoảng thời gian";
        WriteDateTime(sheet.Cell(4, 2), report.Start);
        sheet.Cell(4, 3).Value = "đến";
        sheet.Cell(4, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        WriteDateTime(sheet.Cell(4, 4), report.End);

        WriteMetadata(
            sheet,
            5,
            "Thời điểm xuất",
            TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ReportTimeZone).ToString(DateTimeFormat)
        );

        var header = sheet.Range(7, 1, 7, 4);
        header.Cell(1, 1).Value = "Chỉ số";
        header.Cell(1, 2).Value = "Giá trị";
        header.Cell(1, 3).Value = "Ghi chú";
        header.Range(1, 3, 1, 4).Merge();
        StyleHeader(header);

        var row = 8;

        WriteIntegerMetric(sheet, ref row, "Tổng số bệnh nhân", report.TotalPatients, null);
        WriteIntegerMetric(sheet, ref row, "Tổng số phác đồ", report.TotalTreatmentPlans, null);
        WriteIntegerMetric(
            sheet,
            ref row,
            "Phác đồ tuân thủ",
            report.TotalAdherentPlans,
            "Adhere"
        );
        WriteIntegerMetric(
            sheet,
            ref row,
            "Phác đồ không tuân thủ",
            report.TotalNonAdherentPlans,
            "NotAdhereValid + NotAdhereInvalid"
        );
        WriteIntegerMetric(
            sheet,
            ref row,
            "Phác đồ chờ đánh giá",
            report.TotalPendingReviewPlans,
            "NotAdhere"
        );
        WriteIntegerMetric(
            sheet,
            ref row,
            "Bệnh nhân tử vong",
            report.TotalDeceasedPatients,
            null
        );
        WriteIntegerMetric(
            sheet,
            ref row,
            "Bệnh nhân hồi phục",
            report.TotalRecoveredPatients,
            null
        );
        WriteIntegerMetric(
            sheet,
            ref row,
            "Kết cục khác",
            report.TotalOtherOutcomePatients,
            null
        );
        WritePercentMetric(
            sheet,
            ref row,
            "Tỷ lệ tuân thủ",
            SafeRate(report.TotalAdherentPlans, report.TotalTreatmentPlans),
            null
        );
        WritePercentMetric(
            sheet,
            ref row,
            "Tỷ lệ không tuân thủ",
            SafeRate(report.TotalNonAdherentPlans, report.TotalTreatmentPlans),
            null
        );
        WritePercentMetric(
            sheet,
            ref row,
            "Tỷ lệ chờ đánh giá",
            SafeRate(report.TotalPendingReviewPlans, report.TotalTreatmentPlans),
            null
        );

        var note = sheet.Range(row + 1, 1, row + 1, 4);
        note.Merge();
        note.Value =
            "Ghi chú: Tuân thủ = Adhere; Không tuân thủ = NotAdhereValid + NotAdhereInvalid; "
            + "Chờ đánh giá = NotAdhere.";
        note.Style.Font.Italic = true;
        note.Style.Alignment.WrapText = true;
        note.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sheet.Row(row + 1).Height = 30;

        sheet.Column(1).Width = 28;
        sheet.Column(2).Width = 38;
        sheet.Column(3).Width = 8;
        sheet.Column(4).Width = 20;

        sheet.Style.Font.FontSize = 11;
    }

    private static void WriteDoctorSheet(XLWorkbook workbook, AnalysisReportResult report)
    {
        var sheet = workbook.Worksheets.Add(DoctorSheetName);

        string[] headers =
        [
            "STT",
            "Mã bác sĩ",
            "Tổng bệnh nhân",
            "Tổng phác đồ",
            "Tuân thủ",
            "Không tuân thủ",
            "Chờ đánh giá",
            "Tỷ lệ tuân thủ",
            "Tỷ lệ không tuân thủ",
            "Tỷ lệ chờ đánh giá",
        ];

        WriteHeaderRow(sheet, headers);
        sheet.SheetView.FreezeRows(1);

        var doctors = report.Doctors.OrderBy(doctor => doctor.DoctorId).ToList();
        var row = 2;

        foreach (var doctor in doctors)
        {
            sheet.Cell(row, 1).Value = row - 1;
            sheet.Cell(row, 2).Value = doctor.DoctorId.ToString();
            WriteInteger(sheet.Cell(row, 3), doctor.TotalPatients);
            WriteInteger(sheet.Cell(row, 4), doctor.TotalTreatmentPlans);
            WriteInteger(sheet.Cell(row, 5), doctor.TotalAdherentPlans);
            WriteInteger(sheet.Cell(row, 6), doctor.TotalNonAdherentPlans);
            WriteInteger(sheet.Cell(row, 7), doctor.TotalPendingReviewPlans);
            WritePercent(
                sheet.Cell(row, 8),
                SafeRate(doctor.TotalAdherentPlans, doctor.TotalTreatmentPlans)
            );
            WritePercent(
                sheet.Cell(row, 9),
                SafeRate(doctor.TotalNonAdherentPlans, doctor.TotalTreatmentPlans)
            );
            WritePercent(
                sheet.Cell(row, 10),
                SafeRate(doctor.TotalPendingReviewPlans, doctor.TotalTreatmentPlans)
            );
            row++;
        }

        if (doctors.Count == 0)
        {
            WriteEmptyRow(sheet, row, headers.Length);
        }
        else
        {
            WriteDoctorTotals(sheet, row, doctors);
        }

        FinishSheet(sheet, row, headers.Length, doctors.Count == 0 ? 1 : row - 1);
    }

    private static void WriteMedicineSheet(XLWorkbook workbook, AnalysisReportResult report)
    {
        var sheet = workbook.Worksheets.Add(MedicineSheetName);

        string[] headers =
        [
            "STT",
            "Mã bác sĩ",
            "Mã thuốc",
            "Tên thuốc",
            "Phân loại",
            "Tổng phác đồ",
            "Tuân thủ",
            "Không tuân thủ",
            "Chờ đánh giá",
            "Tỷ lệ tuân thủ",
        ];

        WriteHeaderRow(sheet, headers);
        sheet.SheetView.FreezeRows(1);

        var rows = report
            .Doctors.OrderBy(doctor => doctor.DoctorId)
            .SelectMany(doctor =>
                doctor.Medicines.OrderBy(medicine => medicine.Name)
                    .Select(medicine => (doctor.DoctorId, Medicine: medicine))
            )
            .ToList();

        var row = 2;
        var index = 1;

        foreach (var (doctorId, medicine) in rows)
        {
            sheet.Cell(row, 1).Value = index++;
            sheet.Cell(row, 2).Value = doctorId.ToString();
            sheet.Cell(row, 3).Value = medicine.MedicineId.ToString();
            sheet.Cell(row, 4).Value = medicine.Name;
            sheet.Cell(row, 5).Value = medicine.Classification;
            WriteInteger(sheet.Cell(row, 6), medicine.TotalTreatmentPlans);
            WriteInteger(sheet.Cell(row, 7), medicine.TotalAdherentPlans);
            WriteInteger(sheet.Cell(row, 8), medicine.TotalNonAdherentPlans);
            WriteInteger(sheet.Cell(row, 9), medicine.TotalPendingReviewPlans);
            WritePercent(
                sheet.Cell(row, 10),
                SafeRate(medicine.TotalAdherentPlans, medicine.TotalTreatmentPlans)
            );
            row++;
        }

        if (rows.Count == 0)
        {
            WriteEmptyRow(sheet, row, headers.Length);
        }

        FinishSheet(sheet, row, headers.Length);
    }

    private static void WriteMetadata(IXLWorksheet sheet, int row, string label, string value)
    {
        sheet.Cell(row, 1).Value = label;
        sheet.Cell(row, 1).Style.Font.Bold = true;

        var valueRange = sheet.Range(row, 2, row, 4);
        valueRange.Merge();
        valueRange.Value = value;
    }

    private static void WriteDateTime(IXLCell cell, DateTimeOffset value)
    {
        cell.Value = TimeZoneInfo.ConvertTime(value, ReportTimeZone).DateTime;
        cell.Style.NumberFormat.Format = DateTimeFormat;
    }

    private static void WriteIntegerMetric(
        IXLWorksheet sheet,
        ref int row,
        string label,
        int value,
        string? note
    )
    {
        sheet.Cell(row, 1).Value = label;
        WriteInteger(sheet.Cell(row, 2), value);
        WriteNote(sheet, row, note);
        row++;
    }

    private static void WritePercentMetric(
        IXLWorksheet sheet,
        ref int row,
        string label,
        double value,
        string? note
    )
    {
        sheet.Cell(row, 1).Value = label;
        WritePercent(sheet.Cell(row, 2), value);
        WriteNote(sheet, row, note);
        row++;
    }

    private static void WriteNote(IXLWorksheet sheet, int row, string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return;
        }

        var range = sheet.Range(row, 3, row, 4);
        range.Merge();
        range.Value = note;
        range.Style.Font.Italic = true;
    }

    private static void WriteHeaderRow(IXLWorksheet sheet, IReadOnlyList<string> headers)
    {
        for (var column = 1; column <= headers.Count; column++)
        {
            sheet.Cell(1, column).Value = headers[column - 1];
        }

        StyleHeader(sheet.Range(1, 1, 1, headers.Count));
        sheet.Row(1).Height = 24;
    }

    private static void WriteDoctorTotals(
        IXLWorksheet sheet,
        int row,
        IReadOnlyCollection<DoctorMedicineAnalysisSummary> doctors
    )
    {
        var totalPatients = doctors.Sum(doctor => doctor.TotalPatients);
        var totalPlans = doctors.Sum(doctor => doctor.TotalTreatmentPlans);
        var totalAdherent = doctors.Sum(doctor => doctor.TotalAdherentPlans);
        var totalNonAdherent = doctors.Sum(doctor => doctor.TotalNonAdherentPlans);
        var totalPending = doctors.Sum(doctor => doctor.TotalPendingReviewPlans);

        sheet.Cell(row, 1).Value = string.Empty;
        sheet.Cell(row, 2).Value = "TỔNG CỘNG";
        WriteInteger(sheet.Cell(row, 3), totalPatients);
        WriteInteger(sheet.Cell(row, 4), totalPlans);
        WriteInteger(sheet.Cell(row, 5), totalAdherent);
        WriteInteger(sheet.Cell(row, 6), totalNonAdherent);
        WriteInteger(sheet.Cell(row, 7), totalPending);
        WritePercent(sheet.Cell(row, 8), SafeRate(totalAdherent, totalPlans));
        WritePercent(sheet.Cell(row, 9), SafeRate(totalNonAdherent, totalPlans));
        WritePercent(sheet.Cell(row, 10), SafeRate(totalPending, totalPlans));

        var totalRow = sheet.Range(row, 1, row, 10);
        totalRow.Style.Font.Bold = true;
        totalRow.Style.Fill.BackgroundColor = TotalFill;
        ApplyBorder(totalRow);
    }

    private static void WriteEmptyRow(IXLWorksheet sheet, int row, int columns)
    {
        var range = sheet.Range(row, 1, row, columns);
        range.Merge();
        range.Value = EmptyMessage;
        range.Style.Font.Italic = true;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ApplyBorder(range);
    }

    private static void FinishSheet(
        IXLWorksheet sheet,
        int lastRow,
        int columns,
        int? filterLastRow = null
    )
    {
        var dataRange = sheet.Range(1, 1, Math.Max(lastRow, 1), columns);
        ApplyBorder(dataRange);

        var filterRow = filterLastRow ?? lastRow;
        if (filterRow > 1)
        {
            sheet.Range(1, 1, filterRow, columns).SetAutoFilter();
        }

        sheet.Columns(1, columns).AdjustToContents();

        foreach (var column in sheet.Columns(1, columns))
        {
            if (column.Width > 40)
            {
                column.Width = 40;
            }

            if (column.Width < 10)
            {
                column.Width = 10;
            }
        }
    }

    private static void StyleHeader(IXLRange range)
    {
        range.Style.Font.Bold = true;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Fill.BackgroundColor = HeaderFill;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        range.Style.Alignment.WrapText = true;
        ApplyBorder(range);
    }

    private static void ApplyBorder(IXLRange range)
    {
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    private static void WriteInteger(IXLCell cell, int value)
    {
        cell.Value = value;
        cell.Style.NumberFormat.Format = IntegerFormat;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    private static void WritePercent(IXLCell cell, double value)
    {
        cell.Value = value;
        cell.Style.NumberFormat.Format = PercentFormat;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    private static double SafeRate(int numerator, int denominator) =>
        denominator == 0 ? 0d : (double)numerator / denominator;

    private static TimeZoneInfo ResolveReportTimeZone()
    {
        string[] ids = ["Asia/Ho_Chi_Minh", "SE Asia Standard Time"];

        foreach (var id in ids)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // Try the next platform-specific ID.
            }
            catch (InvalidTimeZoneException)
            {
                // Try the next platform-specific ID.
            }
        }

        return TimeZoneInfo.Utc;
    }
}

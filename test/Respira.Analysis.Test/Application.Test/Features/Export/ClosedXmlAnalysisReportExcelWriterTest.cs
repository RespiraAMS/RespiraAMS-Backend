using ClosedXML.Excel;
using Respira.Analysis.Application.Contracts;
using Respira.Analysis.Infrastructure.Export;

namespace Respira.Analysis.Application.Test.Features.Export;

public class ClosedXmlAnalysisReportExcelWriterTest
{
    private static readonly Guid DoctorA = new("00000000-0000-0000-0000-000000000001");
    private static readonly Guid DoctorB = new("00000000-0000-0000-0000-000000000002");
    private static readonly Guid MedicineA = new("00000000-0000-0000-0000-000000000011");
    private static readonly Guid MedicineB = new("00000000-0000-0000-0000-000000000012");

    private readonly ClosedXmlAnalysisReportExcelWriter _writer = new();

    [Fact]
    public void Write_FullReport_CreatesAllSheetsAndMetrics()
    {
        var report = CreateFullReport();

        using var workbook = Open(_writer.Write(report));

        Assert.Equal(
            ["Tổng quan", "Theo bác sĩ", "Theo thuốc"],
            workbook.Worksheets.Select(sheet => sheet.Name)
        );

        var overview = workbook.Worksheet("Tổng quan");
        Assert.Equal(10, MetricValue(overview, "Tổng số bệnh nhân"));
        Assert.Equal(4, MetricValue(overview, "Tổng số phác đồ"));
        Assert.Equal(2, MetricValue(overview, "Phác đồ tuân thủ"));
        Assert.Equal(1, MetricValue(overview, "Phác đồ không tuân thủ"));
        Assert.Equal(1, MetricValue(overview, "Phác đồ chờ đánh giá"));
        Assert.Equal(1, MetricValue(overview, "Bệnh nhân tử vong"));
        Assert.Equal(7, MetricValue(overview, "Bệnh nhân hồi phục"));
        Assert.Equal(2, MetricValue(overview, "Kết cục khác"));

        var adherence = MetricCell(overview, "Tỷ lệ tuân thủ");
        Assert.Equal(0.5, adherence.GetDouble());
        Assert.Equal("0.0%", adherence.Style.NumberFormat.Format);

        var doctorSheet = workbook.Worksheet("Theo bác sĩ");
        Assert.Equal("Mã bác sĩ", doctorSheet.Cell(1, 2).GetString());
        Assert.Equal(DoctorA.ToString(), doctorSheet.Cell(2, 2).GetString());
        Assert.Equal(DoctorB.ToString(), doctorSheet.Cell(3, 2).GetString());
        Assert.Equal(6, doctorSheet.Cell(2, 3).GetDouble());
        Assert.Equal(0.6666666666666666, doctorSheet.Cell(2, 8).GetDouble(), 10);
        Assert.Equal("TỔNG CỘNG", doctorSheet.Cell(4, 2).GetString());
        Assert.Equal(10, doctorSheet.Cell(4, 3).GetDouble());
        Assert.Equal(4, doctorSheet.Cell(4, 4).GetDouble());

        var medicineSheet = workbook.Worksheet("Theo thuốc");
        Assert.Equal(3, medicineSheet.RowsUsed().Count() - 1);
        Assert.Equal("Amoxicillin", medicineSheet.Cell(2, 4).GetString());
        Assert.Equal("Penicillin", medicineSheet.Cell(2, 5).GetString());
        Assert.Equal("Ceftriaxone", medicineSheet.Cell(3, 4).GetString());
    }

    [Fact]
    public void Write_EmptyReport_WritesEmptyStateWithoutThrowing()
    {
        var report = CreateFullReport() with
        {
            Doctors = [],
            TotalPatients = 0,
            TotalTreatmentPlans = 0,
            TotalAdherentPlans = 0,
            TotalNonAdherentPlans = 0,
            TotalPendingReviewPlans = 0,
            TotalDeceasedPatients = 0,
            TotalRecoveredPatients = 0,
            TotalOtherOutcomePatients = 0,
        };

        using var workbook = Open(_writer.Write(report));

        Assert.Equal("Không có dữ liệu", workbook.Worksheet("Theo bác sĩ").Cell(2, 1).GetString());
        Assert.Equal("Không có dữ liệu", workbook.Worksheet("Theo thuốc").Cell(2, 1).GetString());
        Assert.Equal(0, MetricValue(workbook.Worksheet("Tổng quan"), "Tỷ lệ tuân thủ"));
    }

    [Fact]
    public void Write_DoctorWithoutMedicines_StillWritesDoctorRow()
    {
        var report = CreateFullReport() with
        {
            Doctors =
            [
                new DoctorMedicineAnalysisSummary(DoctorA, 1, 1, 1, 0, 0, []),
            ],
        };

        using var workbook = Open(_writer.Write(report));

        Assert.Equal(DoctorA.ToString(), workbook.Worksheet("Theo bác sĩ").Cell(2, 2).GetString());
        Assert.Equal("Không có dữ liệu", workbook.Worksheet("Theo thuốc").Cell(2, 1).GetString());
    }

    [Fact]
    public void Write_ZeroPlanTotals_DoesNotDivideByZero()
    {
        var report = CreateFullReport() with
        {
            Doctors =
            [
                new DoctorMedicineAnalysisSummary(
                    DoctorA,
                    1,
                    0,
                    0,
                    0,
                    0,
                    [new MedicineAnalysisSummary(MedicineA, "Amoxicillin", "Penicillin", 0, 0, 0, 0)]
                ),
            ],
        };

        using var workbook = Open(_writer.Write(report));

        Assert.Equal(0, workbook.Worksheet("Theo bác sĩ").Cell(2, 8).GetDouble());
        Assert.Equal(0, workbook.Worksheet("Theo thuốc").Cell(2, 10).GetDouble());
    }

    [Fact]
    public void Write_ProducesExactlyTheThreeExpectedSheets()
    {
        using var workbook = Open(_writer.Write(CreateFullReport()));

        Assert.Equal(3, workbook.Worksheets.Count);
        Assert.Equal(
            ["Tổng quan", "Theo bác sĩ", "Theo thuốc"],
            workbook.Worksheets.Select(sheet => sheet.Name)
        );
    }

    [Fact]
    public void Write_OverviewSheet_ContainsReportMetadata()
    {
        var report = CreateFullReport();

        using var workbook = Open(_writer.Write(report));
        var overview = workbook.Worksheet("Tổng quan");

        Assert.Equal("BÁO CÁO PHÂN TÍCH ĐIỀU TRỊ", overview.Cell(1, 1).GetString());
        Assert.Equal("Mã báo cáo", overview.Cell(2, 1).GetString());
        Assert.Equal(report.AnalysisId.ToString(), overview.Cell(2, 2).GetString());
        Assert.Equal("Bác sĩ phụ trách", overview.Cell(3, 1).GetString());
        Assert.Equal(report.DoctorId!.Value.ToString(), overview.Cell(3, 2).GetString());
        Assert.Equal("Khoảng thời gian", overview.Cell(4, 1).GetString());
        Assert.Equal("đến", overview.Cell(4, 3).GetString());
        Assert.Equal(ToReportTime(report.Start), overview.Cell(4, 2).GetDateTime());
        Assert.Equal(ToReportTime(report.End), overview.Cell(4, 4).GetDateTime());
    }

    [Fact]
    public void Write_OverviewSheet_WithoutDoctor_ShowsAllDoctorsLabel()
    {
        var report = CreateFullReport() with { DoctorId = null };

        using var workbook = Open(_writer.Write(report));
        var overview = workbook.Worksheet("Tổng quan");

        Assert.Equal("Tất cả bác sĩ", overview.Cell(3, 2).GetString());
    }

    [Fact]
    public void Write_OverviewSheet_ContainsEveryIntegerMetricRow()
    {
        using var workbook = Open(_writer.Write(CreateFullReport()));
        var overview = workbook.Worksheet("Tổng quan");

        (string Label, double Value)[] expected =
        [
            ("Tổng số bệnh nhân", 10),
            ("Tổng số phác đồ", 4),
            ("Phác đồ tuân thủ", 2),
            ("Phác đồ không tuân thủ", 1),
            ("Phác đồ chờ đánh giá", 1),
            ("Bệnh nhân tử vong", 1),
            ("Bệnh nhân hồi phục", 7),
            ("Kết cục khác", 2),
        ];

        for (var index = 0; index < expected.Length; index++)
        {
            var label = expected[index].Label;
            Assert.Equal(8 + index, MetricRow(overview, label));
            Assert.Equal(expected[index].Value, MetricValue(overview, label));
        }

        Assert.Equal("Adhere", overview.Cell(10, 3).GetString());
        Assert.Equal("NotAdhereValid + NotAdhereInvalid", overview.Cell(11, 3).GetString());
        Assert.Equal("NotAdhere", overview.Cell(12, 3).GetString());
    }

    [Fact]
    public void Write_OverviewSheet_ContainsTheThreePercentageRows()
    {
        using var workbook = Open(_writer.Write(CreateFullReport()));
        var overview = workbook.Worksheet("Tổng quan");

        Assert.Equal(16, MetricRow(overview, "Tỷ lệ tuân thủ"));
        Assert.Equal(17, MetricRow(overview, "Tỷ lệ không tuân thủ"));
        Assert.Equal(18, MetricRow(overview, "Tỷ lệ chờ đánh giá"));

        Assert.Equal(0.5, MetricValue(overview, "Tỷ lệ tuân thủ"));
        Assert.Equal(0.25, MetricValue(overview, "Tỷ lệ không tuân thủ"));
        Assert.Equal(0.25, MetricValue(overview, "Tỷ lệ chờ đánh giá"));
    }

    [Fact]
    public void Write_NoDoctors_WritesEmptyRowOnDoctorSheet()
    {
        var report = CreateFullReport() with { Doctors = [] };

        using var workbook = Open(_writer.Write(report));
        var doctorSheet = workbook.Worksheet("Theo bác sĩ");

        Assert.Equal("STT", doctorSheet.Cell(1, 1).GetString());
        Assert.Equal("Mã bác sĩ", doctorSheet.Cell(1, 2).GetString());
        Assert.Equal("Không có dữ liệu", doctorSheet.Cell(2, 1).GetString());
        Assert.True(string.IsNullOrEmpty(doctorSheet.Cell(2, 2).GetString()));
        Assert.Equal(2, doctorSheet.LastRowUsed()!.RowNumber());
    }

    [Fact]
    public void Write_WithDoctors_WritesOneRowPerDoctorWithCounts()
    {
        using var workbook = Open(_writer.Write(CreateFullReport()));
        var doctorSheet = workbook.Worksheet("Theo bác sĩ");

        Assert.Equal(1, doctorSheet.Cell(2, 1).GetDouble());
        Assert.Equal(DoctorA.ToString(), doctorSheet.Cell(2, 2).GetString());
        Assert.Equal(6, doctorSheet.Cell(2, 3).GetDouble());
        Assert.Equal(3, doctorSheet.Cell(2, 4).GetDouble());
        Assert.Equal(2, doctorSheet.Cell(2, 5).GetDouble());
        Assert.Equal(1, doctorSheet.Cell(2, 6).GetDouble());
        Assert.Equal(0, doctorSheet.Cell(2, 7).GetDouble());

        Assert.Equal(2, doctorSheet.Cell(3, 1).GetDouble());
        Assert.Equal(DoctorB.ToString(), doctorSheet.Cell(3, 2).GetString());
        Assert.Equal(4, doctorSheet.Cell(3, 3).GetDouble());
        Assert.Equal(1, doctorSheet.Cell(3, 4).GetDouble());
        Assert.Equal(0, doctorSheet.Cell(3, 5).GetDouble());
        Assert.Equal(0, doctorSheet.Cell(3, 6).GetDouble());
        Assert.Equal(1, doctorSheet.Cell(3, 7).GetDouble());

        Assert.Equal("TỔNG CỘNG", doctorSheet.Cell(4, 2).GetString());
        Assert.Equal(10, doctorSheet.Cell(4, 3).GetDouble());
        Assert.Equal(4, doctorSheet.Cell(4, 4).GetDouble());
        Assert.Equal(2, doctorSheet.Cell(4, 5).GetDouble());
        Assert.Equal(1, doctorSheet.Cell(4, 6).GetDouble());
        Assert.Equal(1, doctorSheet.Cell(4, 7).GetDouble());
        Assert.Equal(0.5, doctorSheet.Cell(4, 8).GetDouble());
    }

    [Fact]
    public void Write_MedicineSheet_WritesOneRowPerDoctorMedicinePair()
    {
        using var workbook = Open(_writer.Write(CreateFullReport()));
        var medicineSheet = workbook.Worksheet("Theo thuốc");

        Assert.Equal("STT", medicineSheet.Cell(1, 1).GetString());
        Assert.Equal("Tên thuốc", medicineSheet.Cell(1, 4).GetString());
        Assert.Equal("Phân loại", medicineSheet.Cell(1, 5).GetString());

        Assert.Equal(1, medicineSheet.Cell(2, 1).GetDouble());
        Assert.Equal(DoctorA.ToString(), medicineSheet.Cell(2, 2).GetString());
        Assert.Equal(MedicineA.ToString(), medicineSheet.Cell(2, 3).GetString());
        Assert.Equal("Amoxicillin", medicineSheet.Cell(2, 4).GetString());
        Assert.Equal("Penicillin", medicineSheet.Cell(2, 5).GetString());
        Assert.Equal(2, medicineSheet.Cell(2, 6).GetDouble());
        Assert.Equal(2, medicineSheet.Cell(2, 7).GetDouble());
        Assert.Equal(0, medicineSheet.Cell(2, 8).GetDouble());
        Assert.Equal(0, medicineSheet.Cell(2, 9).GetDouble());
        Assert.Equal(1, medicineSheet.Cell(2, 10).GetDouble());

        Assert.Equal(2, medicineSheet.Cell(3, 1).GetDouble());
        Assert.Equal(DoctorA.ToString(), medicineSheet.Cell(3, 2).GetString());
        Assert.Equal(MedicineB.ToString(), medicineSheet.Cell(3, 3).GetString());
        Assert.Equal("Ceftriaxone", medicineSheet.Cell(3, 4).GetString());
        Assert.Equal("Cephalosporin", medicineSheet.Cell(3, 5).GetString());
        Assert.Equal(1, medicineSheet.Cell(3, 6).GetDouble());
        Assert.Equal(0, medicineSheet.Cell(3, 7).GetDouble());
        Assert.Equal(1, medicineSheet.Cell(3, 8).GetDouble());
        Assert.Equal(0, medicineSheet.Cell(3, 9).GetDouble());
        Assert.Equal(0, medicineSheet.Cell(3, 10).GetDouble());

        Assert.Equal(3, medicineSheet.Cell(4, 1).GetDouble());
        Assert.Equal(DoctorB.ToString(), medicineSheet.Cell(4, 2).GetString());
        Assert.Equal(MedicineB.ToString(), medicineSheet.Cell(4, 3).GetString());
        Assert.Equal("Ceftriaxone", medicineSheet.Cell(4, 4).GetString());
        Assert.Equal("Cephalosporin", medicineSheet.Cell(4, 5).GetString());
        Assert.Equal(1, medicineSheet.Cell(4, 6).GetDouble());
        Assert.Equal(0, medicineSheet.Cell(4, 7).GetDouble());
        Assert.Equal(0, medicineSheet.Cell(4, 8).GetDouble());
        Assert.Equal(1, medicineSheet.Cell(4, 9).GetDouble());
        Assert.Equal(0, medicineSheet.Cell(4, 10).GetDouble());
    }

    [Fact]
    public void Write_UsesPercentFormatForRatesAndIntegerFormatForCounts()
    {
        using var workbook = Open(_writer.Write(CreateFullReport()));
        var overview = workbook.Worksheet("Tổng quan");
        var doctorSheet = workbook.Worksheet("Theo bác sĩ");
        var medicineSheet = workbook.Worksheet("Theo thuốc");

        Assert.Equal("0.0%", MetricCell(overview, "Tỷ lệ tuân thủ").Style.NumberFormat.Format);
        Assert.Equal(
            "0.0%",
            MetricCell(overview, "Tỷ lệ không tuân thủ").Style.NumberFormat.Format
        );
        Assert.Equal(
            "0.0%",
            MetricCell(overview, "Tỷ lệ chờ đánh giá").Style.NumberFormat.Format
        );
        Assert.Equal(
            "#,##0",
            MetricCell(overview, "Tổng số bệnh nhân").Style.NumberFormat.Format
        );
        Assert.Equal("#,##0", MetricCell(overview, "Tổng số phác đồ").Style.NumberFormat.Format);

        Assert.Equal("0.0%", doctorSheet.Cell(2, 8).Style.NumberFormat.Format);
        Assert.Equal("#,##0", doctorSheet.Cell(2, 3).Style.NumberFormat.Format);

        Assert.Equal("0.0%", medicineSheet.Cell(2, 10).Style.NumberFormat.Format);
        Assert.Equal("#,##0", medicineSheet.Cell(2, 6).Style.NumberFormat.Format);
    }

    [Fact]
    public void Write_ZeroTotalPlans_AdherenceRateIsZeroAndNotNaN()
    {
        var report = CreateFullReport() with
        {
            TotalTreatmentPlans = 0,
            TotalAdherentPlans = 0,
            TotalNonAdherentPlans = 0,
            TotalPendingReviewPlans = 0,
            Doctors =
            [
                new DoctorMedicineAnalysisSummary(DoctorA, 0, 0, 0, 0, 0, []),
            ],
        };

        using var workbook = Open(_writer.Write(report));
        var overview = workbook.Worksheet("Tổng quan");

        var adherence = MetricValue(overview, "Tỷ lệ tuân thủ");
        Assert.Equal(0, adherence);
        Assert.False(double.IsNaN(adherence));
        Assert.False(double.IsInfinity(adherence));

        var nonAdherence = MetricValue(overview, "Tỷ lệ không tuân thủ");
        Assert.Equal(0, nonAdherence);
        Assert.False(double.IsNaN(nonAdherence));

        var pending = MetricValue(overview, "Tỷ lệ chờ đánh giá");
        Assert.Equal(0, pending);
        Assert.False(double.IsNaN(pending));

        var doctorSheet = workbook.Worksheet("Theo bác sĩ");
        Assert.Equal(0, doctorSheet.Cell(2, 8).GetDouble());
        Assert.Equal(0, doctorSheet.Cell(3, 8).GetDouble());
    }

    [Fact]
    public void Write_EmptyDoctorCollection_WritesEmptyMessageRowOnMedicineSheet()
    {
        var report = CreateFullReport() with { Doctors = [] };

        using var workbook = Open(_writer.Write(report));
        var medicineSheet = workbook.Worksheet("Theo thuốc");

        Assert.Equal("STT", medicineSheet.Cell(1, 1).GetString());
        Assert.Equal("Tên thuốc", medicineSheet.Cell(1, 4).GetString());
        Assert.Equal("Không có dữ liệu", medicineSheet.Cell(2, 1).GetString());
        Assert.Equal(2, medicineSheet.LastRowUsed()!.RowNumber());
    }

    private static AnalysisReportResult CreateFullReport() =>
        new(
            Guid.CreateVersion7(),
            DoctorA,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
            4,
            10,
            2,
            1,
            1,
            1,
            7,
            2,
            [
                new DoctorMedicineAnalysisSummary(
                    DoctorA,
                    6,
                    3,
                    2,
                    1,
                    0,
                    [
                        new MedicineAnalysisSummary(MedicineA, "Amoxicillin", "Penicillin", 2, 2, 0, 0),
                        new MedicineAnalysisSummary(MedicineB, "Ceftriaxone", "Cephalosporin", 1, 0, 1, 0),
                    ]
                ),
                new DoctorMedicineAnalysisSummary(
                    DoctorB,
                    4,
                    1,
                    0,
                    0,
                    1,
                    [new MedicineAnalysisSummary(MedicineB, "Ceftriaxone", "Cephalosporin", 1, 0, 0, 1)]
                ),
            ]
        );

    private static XLWorkbook Open(byte[] content)
    {
        Assert.NotEmpty(content);
        return new XLWorkbook(new MemoryStream(content));
    }

    private static IXLCell MetricCell(IXLWorksheet sheet, string label) =>
        sheet.Cell(MetricRow(sheet, label), 2);

    private static int MetricRow(IXLWorksheet sheet, string label) =>
        sheet
            .Column(1)
            .CellsUsed()
            .First(cell => cell.GetString() == label)
            .Address.RowNumber;

    private static double MetricValue(IXLWorksheet sheet, string label) =>
        MetricCell(sheet, label).GetDouble();

    private static DateTime ToReportTime(DateTimeOffset value) =>
        TimeZoneInfo.ConvertTime(value, ResolveReportTimeZone()).DateTime;

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

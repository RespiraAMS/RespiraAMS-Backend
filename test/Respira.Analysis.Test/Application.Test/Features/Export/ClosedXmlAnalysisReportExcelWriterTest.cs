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

    private static IXLCell MetricCell(IXLWorksheet sheet, string label)
    {
        var row = sheet
            .Column(1)
            .CellsUsed()
            .First(cell => cell.GetString() == label)
            .Address.RowNumber;

        return sheet.Cell(row, 2);
    }

    private static double MetricValue(IXLWorksheet sheet, string label) =>
        MetricCell(sheet, label).GetDouble();
}

using ClosedXML.Excel;
using Respira.Analysis.Application.Contracts;
using Respira.Analysis.Infrastructure.Export;

namespace Respira.Analysis.Infrastructure.Test.Export;

public class ClosedXmlAnalysisReportExcelWriterTest
{
    private static readonly Guid DoctorA = new("00000000-0000-0000-0000-000000000001");
    private static readonly Guid DoctorB = new("00000000-0000-0000-0000-000000000002");
    private static readonly Guid MedicineA = new("00000000-0000-0000-0000-000000000011");
    private static readonly Guid MedicineB = new("00000000-0000-0000-0000-000000000012");

    private readonly ClosedXmlAnalysisReportExcelWriter _writer = new();

    [Fact]
    public void Write_FullReport_CreatesThreeSheetsWithExpectedOrderAndMetrics()
    {
        var report = CreateFullReport();

        using var workbook = Open(_writer.Write(report));

        Assert.Equal(
            ["Tổng quan", "Theo bác sĩ", "Theo thuốc"],
            workbook.Worksheets.Select(sheet => sheet.Name)
        );

        var overview = workbook.Worksheet("Tổng quan");
        Assert.Equal("BÁO CÁO PHÂN TÍCH ĐIỀU TRỊ", overview.Cell(1, 1).GetString());
        Assert.Equal(report.AnalysisId.ToString(), overview.Cell(2, 2).GetString());
        Assert.Equal(DoctorA.ToString(), overview.Cell(3, 2).GetString());
    }

    [Fact]
    public void Write_DoctorAndMedicineSheets_SortDoctorsByIdAndMedicinesByName()
    {
        var report = CreateFullReport() with
        {
            Doctors =
            [
                new DoctorMedicineAnalysisSummary(
                    DoctorB,
                    2,
                    2,
                    1,
                    1,
                    0,
                    [
                        new MedicineAnalysisSummary(MedicineB, "Zithromax", "Macrolide", 1, 1, 0, 0),
                        new MedicineAnalysisSummary(MedicineA, "Augmentin", "Penicillin", 1, 0, 1, 0),
                    ]
                ),
                new DoctorMedicineAnalysisSummary(
                    DoctorA,
                    1,
                    1,
                    1,
                    0,
                    0,
                    [
                        new MedicineAnalysisSummary(MedicineA, "Cefixime", "Cephalosporin", 1, 1, 0, 0),
                    ]
                ),
            ],
        };

        using var workbook = Open(_writer.Write(report));

        var doctorSheet = workbook.Worksheet("Theo bác sĩ");
        Assert.Equal(DoctorA.ToString(), doctorSheet.Cell(2, 2).GetString());
        Assert.Equal(DoctorB.ToString(), doctorSheet.Cell(3, 2).GetString());
        Assert.Equal("TỔNG CỘNG", doctorSheet.Cell(4, 2).GetString());

        var medicineSheet = workbook.Worksheet("Theo thuốc");
        Assert.Equal(DoctorA.ToString(), medicineSheet.Cell(2, 2).GetString());
        Assert.Equal("Cefixime", medicineSheet.Cell(2, 4).GetString());
        Assert.Equal(DoctorB.ToString(), medicineSheet.Cell(3, 2).GetString());
        Assert.Equal("Augmentin", medicineSheet.Cell(3, 4).GetString());
        Assert.Equal(DoctorB.ToString(), medicineSheet.Cell(4, 2).GetString());
        Assert.Equal("Zithromax", medicineSheet.Cell(4, 4).GetString());
    }

    [Fact]
    public void Write_LongMedicineNames_ClampsColumnWidthsBetween10And40()
    {
        var veryLongName = new string('A', 120);
        var report = CreateFullReport() with
        {
            Doctors =
            [
                new DoctorMedicineAnalysisSummary(
                    DoctorA,
                    1,
                    1,
                    1,
                    0,
                    0,
                    [
                        new MedicineAnalysisSummary(
                            MedicineA,
                            veryLongName,
                            "Penicillin",
                            1,
                            1,
                            0,
                            0
                        ),
                    ]
                ),
            ],
        };

        using var workbook = Open(_writer.Write(report));
        var medicineSheet = workbook.Worksheet("Theo thuốc");

        foreach (var col in medicineSheet.Columns(1, 10))
        {
            Assert.InRange(col.Width, 10, 40);
        }
    }

    [Fact]
    public void Write_EmptyDoctors_WritesEmptyStateOnDoctorAndMedicineSheets()
    {
        var report = CreateFullReport() with
        {
            DoctorId = null,
            Doctors = [],
            TotalTreatmentPlans = 0,
            TotalPatients = 0,
            TotalAdherentPlans = 0,
            TotalNonAdherentPlans = 0,
            TotalPendingReviewPlans = 0,
        };

        using var workbook = Open(_writer.Write(report));

        Assert.Equal("Tất cả bác sĩ", workbook.Worksheet("Tổng quan").Cell(3, 2).GetString());
        Assert.Equal("Không có dữ liệu", workbook.Worksheet("Theo bác sĩ").Cell(2, 1).GetString());
        Assert.Equal("Không có dữ liệu", workbook.Worksheet("Theo thuốc").Cell(2, 1).GetString());
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
                        new MedicineAnalysisSummary(
                            MedicineA,
                            "Amoxicillin",
                            "Penicillin",
                            2,
                            2,
                            0,
                            0
                        ),
                    ]
                ),
            ]
        );

    private static XLWorkbook Open(byte[] content)
    {
        Assert.NotEmpty(content);
        return new XLWorkbook(new MemoryStream(content));
    }
}

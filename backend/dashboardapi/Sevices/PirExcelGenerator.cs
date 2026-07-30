using ClosedXML.Excel;

namespace dashboardapi.Services;

public static class PirExcelGenerator
{
    public static byte[] Generate(PirPdfData pdfData)
    {
        var report = pdfData.Report;

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("PIR Raporu");
        worksheet.ShowGridLines = true;

        worksheet.Range("A1:E1").Merge();
        var titleCell = worksheet.Cell("A1");
        titleCell.Value =
            $"{report.ProjectCode} - {report.ProjectName} PIR RAPORU";
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontSize = 14;
        titleCell.Style.Font.FontColor = XLColor.FromArgb(15, 23, 42);

        AddDetailRow(worksheet, 3, "Proje Kodu", report.ProjectCode ?? "-");
        AddDetailRow(worksheet, 4, "Proje Adı", report.ProjectName ?? "-");
        AddDetailRow(worksheet, 5, "Rapor Dönemi", report.Period ?? "-");
        AddDetailRow(
            worksheet,
            6,
            "Rapor Tarihi",
            report.ReportDate?.ToString("dd.MM.yyyy") ?? "-");
        AddDetailRow(
            worksheet,
            7,
            "Proje Sağlığı",
            report.ManualHealth ?? "-");
        AddDetailRow(
            worksheet,
            8,
            "Rapor Statüsü",
            report.ReportStatus ?? "-");

        var performanceHeader = worksheet.Range("A10:E10");
        performanceHeader.Merge();
        performanceHeader.FirstCell().Value =
            "Proje Performans ve Bütçe Özet Bilgileri";
        performanceHeader.Style.Font.Bold = true;
        performanceHeader.Style.Font.FontColor = XLColor.White;
        performanceHeader.Style.Fill.BackgroundColor =
            XLColor.FromArgb(30, 58, 138);

        string[] headers =
        [
            "Toplam Bütçe (BAC)",
            "Planlanan %",
            "Gerçekleşen %",
            "CPI",
            "SPI"
        ];

        for (var index = 0; index < headers.Length; index++)
        {
            var cell = worksheet.Cell(11, index + 1);
            cell.Value = headers[index];
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;
            cell.Style.Fill.BackgroundColor =
                XLColor.FromArgb(226, 232, 240);
        }

        worksheet.Cell(12, 1).Value =
            $"{pdfData.Bac:N0} {pdfData.Currency}";
        worksheet.Cell(12, 2).Value =
            $"%{pdfData.PlannedProgress:F1}";
        worksheet.Cell(12, 3).Value =
            $"%{pdfData.ActualProgress:F1}";
        worksheet.Cell(12, 4).Value = pdfData.Cpi.ToString("F2");
        worksheet.Cell(12, 5).Value = pdfData.Spi.ToString("F2");

        for (var column = 1; column <= 5; column++)
        {
            var cell = worksheet.Cell(12, column);
            cell.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;
            cell.Style.Border.BottomBorder =
                XLBorderStyleValues.Thin;
            cell.Style.Border.BottomBorderColor =
                XLColor.FromArgb(203, 213, 225);
        }

        var currentRow = 14;
        AddSection(
            worksheet,
            ref currentRow,
            "1. Yönetici Özeti",
            report.ExecutiveSummary);
        AddSection(
            worksheet,
            ref currentRow,
            "2. Tamamlanan İşler",
            report.CompletedWork);

        if (!string.IsNullOrEmpty(report.Delays))
        {
            AddSection(
                worksheet,
                ref currentRow,
                "3. Gecikmeler ve Darboğazlar",
                report.Delays);
        }

        AddSection(
            worksheet,
            ref currentRow,
            "4. Gelecek Dönem Planı",
            report.NextPeriodPlan);

        if (!string.IsNullOrEmpty(report.ManagementExpectations))
        {
            AddSection(
                worksheet,
                ref currentRow,
                "5. Yönetimden Beklentiler",
                report.ManagementExpectations);
        }

        worksheet.Column(1).Width = 22;
        worksheet.Column(2).Width = 18;
        worksheet.Column(3).Width = 18;
        worksheet.Column(4).Width = 12;
        worksheet.Column(5).Width = 12;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void AddDetailRow(
        IXLWorksheet worksheet,
        int row,
        string label,
        string value)
    {
        var labelCell = worksheet.Cell(row, 1);
        labelCell.Value = label;
        labelCell.Style.Font.Bold = true;
        labelCell.Style.Fill.BackgroundColor =
            XLColor.FromArgb(241, 245, 249);

        worksheet.Range(row, 2, row, 5).Merge();
        worksheet.Cell(row, 2).Value = value;
    }

    private static void AddSection(
        IXLWorksheet worksheet,
        ref int row,
        string title,
        string? content)
    {
        var header = worksheet.Range(row, 1, row, 5);
        header.Merge();
        header.FirstCell().Value = title;
        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Fill.BackgroundColor =
            XLColor.FromArgb(30, 58, 138);
        row++;

        var body = worksheet.Range(row, 1, row, 5);
        body.Merge();
        body.FirstCell().Value = content ?? "-";
        body.Style.Alignment.WrapText = true;
        row += 2;
    }
}

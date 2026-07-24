using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using dashboardapi.Models;

namespace dashboardapi.Services;

public static class PirExcelGenerator
{
    public static byte[] Generate(PirPdfData pdfData)
    {
        var report = pdfData.Report;

        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("PIR Raporu");

        // 📌 1. Izgara Çizgilerini Aç
        ws.View.ShowHesaplanamadıdLines = true;

        // 📌 2. BAŞLIK ALANI
        ws.Cells["A1:E1"].Merge = true;
        ws.Cells["A1"].Value = $"{report.ProjectCode} - {report.ProjectName} PIR RAPORU";
        ws.Cells["A1"].Style.Font.Bold = true;
        ws.Cells["A1"].Style.Font.Size = 14;
        ws.Cells["A1"].Style.Font.Color.SetColor(Color.FromArgb(15, 23, 42));

        // 📌 3. KÜNYE BİLGİLERİ
        AddDetailRow(ws, 3, "Proje Kodu", report.ProjectCode ?? "-");
        AddDetailRow(ws, 4, "Proje Adı", report.ProjectName ?? "-");
        AddDetailRow(ws, 5, "Rapor Dönemi", report.Period ?? "-");
        AddDetailRow(ws, 6, "Rapor Tarihi", report.ReportDate?.ToString("dd.MM.yyyy") ?? "-");
        AddDetailRow(ws, 7, "Proje Sağlığı", report.ManualHealth ?? "-");
        AddDetailRow(ws, 8, "Rapor Statüsü", report.ReportStatus ?? "-");

        // 📊 4. BÜTÇE & EVM PERFORMANS TABLOSU
        ws.Cells["A10:E10"].Merge = true;
        ws.Cells["A10"].Value = "Proje Performans ve Bütçe Özet Bilgileri";
        ws.Cells["A10"].Style.Font.Bold = true;
        ws.Cells["A10"].Style.Font.Color.SetColor(Color.White);
        ws.Cells["A10"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        ws.Cells["A10"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(30, 58, 138));

        // Tablo Başlıkları
        string[] headers = { "Toplam Bütçe (BAC)", "Planlanan %", "Gerçekleşen %", "CPI", "SPI" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cells[11, i + 1];
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            // DÜZELTİLEN SATIR 1
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(226, 232, 240));
        }

        // Tablo Değerleri
        ws.Cells[12, 1].Value = $"{pdfData.Bac:N0} {pdfData.Currency}";
        ws.Cells[12, 2].Value = $"%{pdfData.PlannedProgress:F1}";
        ws.Cells[12, 3].Value = $"%{pdfData.ActualProgress:F1}";
        ws.Cells[12, 4].Value = pdfData.Cpi.ToString("F2");
        ws.Cells[12, 5].Value = pdfData.Spi.ToString("F2");

        for (int i = 1; i <= 5; i++)
        {
            // DÜZELTİLEN SATIR 2
            ws.Cells[12, i].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells[12, i].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            ws.Cells[12, i].Style.Border.Bottom.Color.SetColor(Color.FromArgb(203, 213, 225));
        }

        // 📝 5. METİNSEL AÇIKLAMALAR
        int currentRow = 14;

        AddSection(ws, ref currentRow, "1. Yönetici Özeti", report.ExecutiveSummary);
        AddSection(ws, ref currentRow, "2. Tamamlanan İşler", report.CompletedWork);
        
        if (!string.IsNullOrEmpty(report.Delays))
            AddSection(ws, ref currentRow, "3. Gecikmeler ve Darboğazlar", report.Delays);

        AddSection(ws, ref currentRow, "4. Gelecek Dönem Planı", report.NextPeriodPlan);

        if (!string.IsNullOrEmpty(report.ManagementExpectations))
            AddSection(ws, ref currentRow, "5. Yönetimden Beklentiler", report.ManagementExpectations);

        // 📌 6. SÜTUN GENİŞLİKLERİ
        ws.Column(1).Width = 22;
        ws.Column(2).Width = 18;
        ws.Column(3).Width = 18;
        ws.Column(4).Width = 12;
        ws.Column(5).Width = 12;

        return package.GetAsByteArray();
    }

    private static void AddDetailRow(ExcelWorksheet ws, int row, string label, string value)
    {
        ws.Cells[row, 1].Value = label;
        ws.Cells[row, 1].Style.Font.Bold = true;
        ws.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
        ws.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(241, 245, 249));

        ws.Cells[row, 2, row, 5].Merge = true;
        ws.Cells[row, 2].Value = value;
    }

    private static void AddSection(ExcelWorksheet ws, ref int row, string title, string? content)
    {
        ws.Cells[row, 1, row, 5].Merge = true;
        ws.Cells[row, 1].Value = title;
        ws.Cells[row, 1].Style.Font.Bold = true;
        ws.Cells[row, 1].Style.Font.Color.SetColor(Color.White);
        ws.Cells[row, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
        ws.Cells[row, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(30, 58, 138));
        row++;

        ws.Cells[row, 1, row, 5].Merge = true;
        ws.Cells[row, 1].Value = content ?? "-";
        ws.Cells[row, 1].Style.WrapText = true;
        row += 2;
    }
}
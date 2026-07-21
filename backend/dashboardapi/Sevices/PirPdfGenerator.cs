using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using dashboardapi.Models;

namespace dashboardapi.Services;

public record PirPdfData(
    VwPir Report,
    decimal Bac = 0,
    decimal PlannedProgress = 0,
    decimal ActualProgress = 0,
    decimal Cpi = 1.00m,
    decimal Spi = 1.00m,
    string Currency = "TRY",
    byte[]? LogoBytes = null
);

public static class PirPdfGenerator
{
    public static byte[] Generate(PirPdfData pdfData)
    {
        var report = pdfData.Report;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                // 📌 HEADER (Logo ve Proje Başlığı)
                page.Header().Element(ComposeHeader);

                // 📌 CONTENT (Gövde, Tablolar ve Rapor Detayı)
                page.Content().Element(ComposeContent);

                // 📌 FOOTER (Gizlilik Notu & Sayfa Numarası)
                page.Footer().Element(ComposeFooter);

                // --- İÇ BİLEŞENLER ---

                void ComposeHeader(IContainer container)
                {
                    container.Row(row =>
                    {
                        // 1. Şirket Logosu (Eğer yüklendiyse gösterir, yoksa alan ayırır)
                        if (pdfData.LogoBytes != null && pdfData.LogoBytes.Length > 0)
                        {
                            row.ConstantItem(120).MaxHeight(50).Image(pdfData.LogoBytes);
                        }

                        // 2. Proje Başlık ve Künye Bilgileri
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().AlignRight().Text($"{report.ProjectCode} - {report.ProjectName}")
                                .FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                            
                            column.Item().AlignRight().Text($"Dönem: {report.Period} | Rapor Tarihi: {report.ReportDate:dd.MM.yyyy}")
                                .FontSize(10).FontColor(Colors.Grey.Darken1);
                            
                            column.Item().AlignRight().Text($"Proje Sağlığı: {report.ManualHealth} | Statü: {report.ReportStatus}")
                                .FontSize(9).SemiBold().FontColor(Colors.Grey.Darken2);
                        });
                    });
                }

                void ComposeContent(IContainer container)
                {
                    container.PaddingVertical(1, Unit.Centimetre).Column(column =>
                    {
                        column.Spacing(12);

                        // 📊 BÖLÜM 1: EVM & BÜTÇE İLERLEME TABLOSU
                        column.Item().Text("Proje Performans ve Bütçe Özet Bilgileri").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                        
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); // BAC
                                columns.RelativeColumn(1.5f); // Planlanan %
                                columns.RelativeColumn(1.5f); // Gerçekleşen %
                                columns.RelativeColumn(1); // CPI
                                columns.RelativeColumn(1); // SPI
                            });

                            // Tablo Başlığı
                           table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignCenter().Text("Toplam Bütçe (BAC)").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignCenter().Text("Planlanan %").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignCenter().Text("Gerçekleşen %").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignCenter().Text("CPI").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Darken3).Padding(5).AlignCenter().Text("SPI").FontColor(Colors.White).Bold();
                            });

                                // Tablo Değerleri
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text($"{pdfData.Bac:N0} {pdfData.Currency}");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text($"%{pdfData.PlannedProgress:F1}");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text($"%{pdfData.ActualProgress:F1}");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text($"{pdfData.Cpi:F2}");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text($"{pdfData.Spi:F2}");
                            });

                        // 📝 BÖLÜM 2: METİNSEL AÇIKLAMALAR
                        AddSection(column, "1. Yönetici Özeti", report.ExecutiveSummary);
                        AddSection(column, "2. Tamamlanan İşler", report.CompletedWork);

                        if (!string.IsNullOrEmpty(report.Delays))
                            AddSection(column, "3. Gecikmeler ve Darboğazlar", report.Delays);

                        AddSection(column, "4. Gelecek Dönem Planı", report.NextPeriodPlan);

                        if (!string.IsNullOrEmpty(report.ManagementExpectations))
                            AddSection(column, "5. Yönetimden Beklentiler", report.ManagementExpectations);

                        // ✍️ BÖLÜM 3: KURUMSAL İMZA / ONAY ALANI
                        column.Item().PaddingTop(20).Row(row =>
                        {
                            // Hazırlayan
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Raporu Hazırlayan").Bold().FontSize(10).FontColor(Colors.Grey.Darken3);
                                c.Item().Text("Proje Yöneticisi").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().PaddingTop(35).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                                c.Item().Text("İmza / Tarih").FontSize(8).FontColor(Colors.Grey.Medium);
                            });

                            row.ConstantItem(60); // İki imza arası boşluk

                            // Onaylayan
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Onaylayan").Bold().FontSize(10).FontColor(Colors.Grey.Darken3);
                                c.Item().Text("PMO / Üst Yönetim").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().PaddingTop(35).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                                c.Item().Text("İmza / Tarih").FontSize(8).FontColor(Colors.Grey.Medium);
                            });
                        });
                    });
                }

                void AddSection(ColumnDescriptor column, string title, string? content)
                {
                    column.Item().Text(title).FontSize(11).Bold().FontColor(Colors.Blue.Darken3);
                    column.Item().PaddingBottom(2).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                    column.Item().Text(content ?? "-").FontSize(9.5f);
                }

                void ComposeFooter(IContainer container)
                {
                    container.Row(row =>
                    {
                        row.RelativeItem().Text("Gizli - Şirket İçi Kullanıma Özeldir")
                            .FontSize(8).FontColor(Colors.Grey.Medium);

                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Sayfa ").FontSize(8).FontColor(Colors.Grey.Medium);
                            x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                            x.Span(" / ").FontSize(8).FontColor(Colors.Grey.Medium);
                            x.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                    });
                }
            });
        });

        return document.GeneratePdf();
    }
}
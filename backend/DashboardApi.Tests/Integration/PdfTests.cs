using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using dashboardapi.Data;
using dashboardapi.DTOs;
using DashboardApi.Tests.Builders;
using DashboardApi.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace DashboardApi.Tests.Integration;

public sealed class PdfTests
{
    [Fact]
    public async Task ExportPirPdf_ExistingReport_ReturnsValidPdfFile()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var credentials = await CreateActiveUserAsync(
            factory,
            role: "Sistem Yöneticisi");

        var loginRequest = new LoginRequest(
            credentials.User.Email,
            credentials.PlainTextPassword);

        using var loginResponse = await client.PostAsJsonAsync(
            "/auth/login",
            loginRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var loginResult =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(loginResult);
        Assert.False(
            string.IsNullOrWhiteSpace(loginResult.Token));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginResult.Token);

        // Act
        using var response = await client.GetAsync(
            "/pirs/PIR-01/export/pdf");

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "application/pdf",
            response.Content.Headers.ContentType?.MediaType);

        var contentDisposition =
            response.Content.Headers.ContentDisposition;

        Assert.NotNull(contentDisposition);

        var fileName = contentDisposition.FileNameStar
            ?? contentDisposition.FileName;

        Assert.NotNull(fileName);

        fileName = fileName.Trim('"');

        Assert.Equal(
            "PRJ-001_PIR_2026-06.pdf",
            fileName);

        var pdfBytes =
            await response.Content.ReadAsByteArrayAsync();

        Assert.NotEmpty(pdfBytes);

        /*
         * PDF dosyaları "%PDF-" imzasıyla başlar.
         * En az ilk 5 byte kontrol edilir.
         */
        Assert.True(
            pdfBytes.Length >= 5,
            "Oluşturulan PDF beklenenden çok kısa.");

        var pdfSignature = Encoding.ASCII.GetString(
            pdfBytes,
            0,
            5);

        Assert.Equal(
            "%PDF-",
            pdfSignature);

        /*
         * Yalnız boş bir PDF başlığı dönmediğini kontrol etmek için
         * makul bir minimum dosya boyutu bekleniyor.
         */
        Assert.True(
            pdfBytes.Length > 500,
            $"Oluşturulan PDF çok küçük: {pdfBytes.Length} byte.");
    }

    private static async Task<TestUserCredentials> CreateActiveUserAsync(
        TestWebApplicationFactory factory,
        string role)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var builder = new TestDataBuilder(db);

        return await builder.CreateActiveUserAsync(role);
    }
}
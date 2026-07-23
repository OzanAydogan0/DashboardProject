using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace DashboardApi.Tests.Fixtures;

public static class SqlScriptRunner
{
    public static async Task RunAsync(
        SqliteConnection connection,
        string scriptPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException(
                $"Test SQL scripti bulunamadı: {scriptPath}",
                scriptPath);
        }

        if (connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException(
                "SQLite bağlantısı açık değil.");
        }

        var script = await File.ReadAllTextAsync(
            scriptPath,
            cancellationToken);

        /*
         * Script içindeki PRAGMA foreign_keys satırlarını kaldırıyoruz.
         * Test DB kurulumu boyunca FK kontrolünü bağlantı seviyesinde
         * kapalı tutuyoruz.
         */
        script = Regex.Replace(
            script,
            @"^\s*PRAGMA\s+foreign_keys\s*=\s*(ON|OFF)\s*;.*$",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        await ExecuteAsync(
            connection,
            "PRAGMA foreign_keys = OFF;",
            cancellationToken);

        await ExecuteAsync(
            connection,
            script,
            cancellationToken);

        await ExecuteAsync(
            connection,
            "PRAGMA foreign_keys = ON;",
            cancellationToken);

        await CheckForeignKeysAsync(
            connection,
            cancellationToken);
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task CheckForeignKeysAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_key_check;";

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return;
        }

        var table = reader.GetString(0);
        var rowId = reader.IsDBNull(1)
            ? "bilinmiyor"
            : reader.GetValue(1).ToString();

        var referencedTable = reader.IsDBNull(2)
            ? "bilinmiyor"
            : reader.GetString(2);

        throw new InvalidOperationException(
            $"SQL seed verisinde foreign key ihlali var. " +
            $"Tablo: {table}, Satır: {rowId}, " +
            $"Referans tablo: {referencedTable}.");
    }
}
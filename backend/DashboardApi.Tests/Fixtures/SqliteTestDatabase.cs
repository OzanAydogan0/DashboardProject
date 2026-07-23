using Microsoft.Data.Sqlite;

namespace DashboardApi.Tests.Fixtures;

public sealed class SqliteTestDatabase : IAsyncDisposable
{
    private bool _initialized;

    public SqliteTestDatabase()
    {
        Connection = new SqliteConnection("Data Source=:memory:");
    }

    public SqliteConnection Connection { get; }

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        await Connection.OpenAsync(cancellationToken);

        var scriptPath = Path.Combine(
            AppContext.BaseDirectory,
            "TestAssets",
            "database.sql");

        await SqlScriptRunner.RunAsync(
            Connection,
            scriptPath,
            cancellationToken);

        _initialized = true;
    }

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
    }
}
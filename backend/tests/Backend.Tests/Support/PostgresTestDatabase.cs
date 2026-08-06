using backend.Data;
using backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Backend.Tests.Support;

public sealed class PostgresTestDatabase : IAsyncLifetime
{
    private const string DefaultAdminConnectionString =
        "Host=localhost;Port=5432;Database=postgres;Username=deadmans;Password=deadmans_dev_password;SSL Mode=Disable";

    private readonly string _databaseName = $"deadmans_tests_{Guid.NewGuid():N}";
    private string? _adminConnectionString;
    private NpgsqlDataSource? _dataSource;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _adminConnectionString = Environment.GetEnvironmentVariable("DEADMANS_TEST_POSTGRES")
            ?? DefaultAdminConnectionString;
        var appBuilder = new NpgsqlConnectionStringBuilder(_adminConnectionString)
        {
            Database = _databaseName
        };
        ConnectionString = appBuilder.ConnectionString;
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(ConnectionString);
        dataSourceBuilder.EnableDynamicJson();
        _dataSource = dataSourceBuilder.Build();

        await using (var admin = new NpgsqlConnection(_adminConnectionString))
        {
            await admin.OpenAsync();
            await using var create = admin.CreateCommand();
            create.CommandText = $"""CREATE DATABASE {QuoteIdentifier(_databaseName)}""";
            await create.ExecuteNonQueryAsync();
        }

        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (string.IsNullOrWhiteSpace(_adminConnectionString))
        {
            return;
        }

        if (_dataSource is not null)
        {
            await _dataSource.DisposeAsync();
        }

        await using var admin = new NpgsqlConnection(_adminConnectionString);
        await admin.OpenAsync();

        await using (var terminate = admin.CreateCommand())
        {
            terminate.CommandText =
                "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @database_name AND pid <> pg_backend_pid()";
            terminate.Parameters.AddWithValue("database_name", _databaseName);
            await terminate.ExecuteNonQueryAsync();
        }

        await using var drop = admin.CreateCommand();
        drop.CommandText = $"""DROP DATABASE IF EXISTS {QuoteIdentifier(_databaseName)}""";
        await drop.ExecuteNonQueryAsync();
    }

    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                _dataSource ?? throw new InvalidOperationException("Postgres test database is not initialized."),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history")
            )
            .ReplaceService<IHistoryRepository, SnakeCaseNpgsqlHistoryRepository>()
            .Options;

        return new ApplicationDbContext(options);
    }

    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT string_agg(format('%I.%I', schemaname, tablename), ', ')
            FROM pg_tables
            WHERE schemaname = 'public' AND tablename <> '__ef_migrations_history'
            """;
        var tableList = (string?)await command.ExecuteScalarAsync();
        if (string.IsNullOrWhiteSpace(tableList))
        {
            return;
        }

        await using var truncate = connection.CreateCommand();
        truncate.CommandText = $"TRUNCATE TABLE {tableList} RESTART IDENTITY CASCADE";
        await truncate.ExecuteNonQueryAsync();
    }

    private static string QuoteIdentifier(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";
}

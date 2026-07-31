using BookCatalog.Api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BookCatalog.Api.Tests;

/// <summary>
/// SQLite в памяти на время одного теста. Берём именно SQLite, а не InMemory-провайдер,
/// чтобы запросы реально транслировались в SQL — как в приложении.
/// База живёт, пока открыто соединение, поэтому держим его до Dispose.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public TestDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = CreateContext();
        db.Database.EnsureCreated();
    }

    /// <summary>Новый контекст на ту же базу — чтобы тест не читал из кеша предыдущей записи.</summary>
    public AppDbContext CreateContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}

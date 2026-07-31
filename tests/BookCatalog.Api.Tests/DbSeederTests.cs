using BookCatalog.Api.Data;
using BookCatalog.Api.Models;
using Xunit;

namespace BookCatalog.Api.Tests;

public class DbSeederTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public void Seed_ПустаяБаза_ДобавляетПримеры()
    {
        using var db = _database.CreateContext();

        DbSeeder.Seed(db);

        Assert.NotEmpty(db.Books);
        Assert.All(db.Books, book =>
        {
            Assert.False(string.IsNullOrWhiteSpace(book.Title));
            Assert.False(string.IsNullOrWhiteSpace(book.Author));
        });
    }

    [Fact]
    public void Seed_ВызванДважды_НеДублируетКниги()
    {
        int afterFirst;
        using (var db = _database.CreateContext())
        {
            DbSeeder.Seed(db);
            afterFirst = db.Books.Count();
        }

        using (var db = _database.CreateContext())
        {
            DbSeeder.Seed(db);
        }

        using (var db = _database.CreateContext())
        {
            Assert.Equal(afterFirst, db.Books.Count());
        }
    }

    [Fact]
    public void Seed_БазаНеПуста_НичегоНеДобавляет()
    {
        using (var db = _database.CreateContext())
        {
            db.Books.Add(new Book
            {
                Title = "Своя книга",
                Author = "Свой автор",
                CreatedAt = DateTime.UtcNow,
            });
            db.SaveChanges();
        }

        using (var db = _database.CreateContext())
        {
            DbSeeder.Seed(db);
        }

        using (var db = _database.CreateContext())
        {
            Assert.Single(db.Books);
            Assert.Equal("Своя книга", db.Books.Single().Title);
        }
    }
}

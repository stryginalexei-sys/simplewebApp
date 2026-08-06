using BookCatalog.Api.Data;
using BookCatalog.Api.Dtos;
using BookCatalog.Api.Models;
using BookCatalog.Api.Services;
using Xunit;

namespace BookCatalog.Api.Tests;

public class BookServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private BookService CreateService() => new(_database.CreateContext());

    /// <summary>Три книги с однозначным порядком сортировки: Аркадий → Михаил → Стивен.</summary>
    private void SeedThreeBooks()
    {
        using var db = _database.CreateContext();
        db.Books.AddRange(
            new Book { Title = "Пикник на обочине", Author = "Аркадий Стругацкий", Genre = "Фантастика", Year = 1972, CreatedAt = DateTime.UtcNow, Pages = 0 },
            new Book { Title = "Мастер и Маргарита", Author = "Михаил Булгаков", Genre = "Роман", Year = 1967, Rating = 5, CreatedAt = DateTime.UtcNow, Pages = 230 },
            new Book { Title = "Краткая история времени", Author = "Стивен Хокинг", Genre = "Научпоп", Year = 1988, CreatedAt = DateTime.UtcNow });
        db.SaveChanges();
    }

    // ---- Чтение и сортировка ----

    [Fact]
    public async Task GetAllAsync_ПустаяБаза_ПустойСписок()
    {
        var result = await CreateService().GetAllAsync(null, null);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_БезФильтров_ВсеКнигиПоАвтору()
    {
        SeedThreeBooks();

        var result = await CreateService().GetAllAsync(null, null);

        Assert.Equal(
            new[] { "Аркадий Стругацкий", "Михаил Булгаков", "Стивен Хокинг" },
            result.Select(b => b.Author));
    }

    [Fact]
    public async Task GetAllAsync_БезФильтров_ВсеКнигиПоГоду()
    {
        SeedThreeBooks();

        var result = await CreateService().GetSortByAsync("year");

        Assert.Equal(
            new[] { "Михаил Булгаков", "Аркадий Стругацкий", "Стивен Хокинг" },
            result.Select(b => b.Author));
    }

    [Fact]
    public async Task GetAllAsync_БезФильтров_ВсеКнигиПоНазванию()
    {
        SeedThreeBooks();

        var result = await CreateService().GetSortByAsync("title");

        Assert.Equal(
            new[] { "Стивен Хокинг" ,"Михаил Булгаков","Аркадий Стругацкий" },
            result.Select(b => b.Author));
    }

    [Fact]
    public async Task GetAllAsync_БезФильтров_ВсеКнигиПоРейтингу()
    {
        SeedThreeBooks();

        var result = await CreateService().GetSortByAsync("rating");

        Assert.Equal(
            new[] {"Михаил Булгаков", "Аркадий Стругацкий", "Стивен Хокинг" },
            result.Select(b => b.Author));
    }

    [Fact]
    public async Task GetAllAsync_ОдинАвтор_СортировкаПоНазванию()
    {
        using (var db = _database.CreateContext())
        {
            db.Books.AddRange(
                new Book { Title = "Второе", Author = "Автор", CreatedAt = DateTime.UtcNow },
                new Book { Title = "Абзац", Author = "Автор", CreatedAt = DateTime.UtcNow });
            db.SaveChanges();
        }

        var result = await CreateService().GetAllAsync(null, null);

        Assert.Equal(new[] { "Абзац", "Второе" }, result.Select(b => b.Title));
    }

    // ---- Поиск ----

    [Fact]
    public async Task GetAllAsync_ПоискПоНазванию_НаходитПодстроку()
    {
        SeedThreeBooks();

        var result = await CreateService().GetAllAsync("Мастер", null);

        Assert.Single(result);
        Assert.Equal("Мастер и Маргарита", result[0].Title);
    }

    [Fact]
    public async Task GetAllAsync_ПоискПоАвтору_НаходитПодстроку()
    {
        SeedThreeBooks();

        var result = await CreateService().GetAllAsync("Хокинг", null);

        Assert.Single(result);
        Assert.Equal("Стивен Хокинг", result[0].Author);
    }

    [Theory]
    [InlineData("булгаков")]
    [InlineData("БУЛГАКОВ")]
    [InlineData("БулГаКоВ")]
    public async Task GetAllAsync_ПоискНеЗависитОтРегистраКириллицы(string term)
    {
        SeedThreeBooks();

        var result = await CreateService().GetAllAsync(term, null);

        Assert.Single(result);
        Assert.Equal("Михаил Булгаков", result[0].Author);
    }

    [Fact]
    public async Task GetAllAsync_ПоискСПробеламиПоКраям_ОбрезаетИхПередСравнением()
    {
        SeedThreeBooks();

        var result = await CreateService().GetAllAsync("  Пикник  ", null);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetAllAsync_ПоискБезСовпадений_ПустойСписок()
    {
        SeedThreeBooks();

        var result = await CreateService().GetAllAsync("Толстой", null);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAllAsync_ПустойПоиск_ФильтрНеПрименяется(string search)
    {
        SeedThreeBooks();

        var result = await CreateService().GetAllAsync(search, null);

        Assert.Equal(3, result.Count);
    }

    // ---- Фильтр по жанру ----

    [Fact]
    public async Task GetAllAsync_ФильтрПоЖанру_ТолькоЭтотЖанр()
    {
        SeedThreeBooks();

        var result = await CreateService().GetAllAsync(null, "Роман");

        Assert.Single(result);
        Assert.Equal("Роман", result[0].Genre);
    }

    [Fact]
    public async Task GetAllAsync_ФильтрПоЖанру_ТребуетТочногоСовпадения()
    {
        SeedThreeBooks();

        var result = await CreateService().GetAllAsync(null, "Рома");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_ПоискИЖанрВместе_ПрименяютсяОба()
    {
        SeedThreeBooks();

        var совпадает = await CreateService().GetAllAsync("Мастер", "Роман");
        var неСовпадает = await CreateService().GetAllAsync("Мастер", "Фантастика");

        Assert.Single(совпадает);
        Assert.Empty(неСовпадает);
    }

    // ---- GetByIdAsync ----

    [Fact]
    public async Task GetByIdAsync_КнигаЕсть_ВозвращаетЕё()
    {
        var created = await CreateService().CreateAsync(NewInput());

        var found = await CreateService().GetByIdAsync(created.Id);

        Assert.NotNull(found);
        Assert.Equal(created.Id, found.Id);
        Assert.Equal("Мастер и Маргарита", found.Title);
    }

    [Fact]
    public async Task GetByIdAsync_КнигиНет_Null()
    {
        var found = await CreateService().GetByIdAsync(404);

        Assert.Null(found);
    }

    // ---- CreateAsync ----

    [Fact]
    public async Task CreateAsync_СохраняетКнигуИВыдаётId()
    {
        var created = await CreateService().CreateAsync(NewInput());

        Assert.True(created.Id > 0);

        using var db = _database.CreateContext();
        Assert.Equal(1, db.Books.Count());
    }

    [Fact]
    public async Task CreateAsync_ОбрезаетПробелыВНазванииИАвторе()
    {
        var input = NewInput(title: "  Мастер и Маргарита  ", author: "  Михаил Булгаков  ");

        var created = await CreateService().CreateAsync(input);

        Assert.Equal("Мастер и Маргарита", created.Title);
        Assert.Equal("Михаил Булгаков", created.Author);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_ПустойЖанр_СохраняетсяКакNull(string genre)
    {
        var created = await CreateService().CreateAsync(NewInput(genre: genre));

        Assert.Null(created.Genre);
    }

    [Fact]
    public async Task CreateAsync_ПустоеОписание_СохраняетсяКакNull()
    {
        var created = await CreateService().CreateAsync(NewInput(description: "  "));

        Assert.Null(created.Description);
    }

    [Fact]
    public async Task CreateAsync_ЗаполняетCreatedAt()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var created = await CreateService().CreateAsync(NewInput());

        Assert.InRange(created.CreatedAt, before, DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task CreateAsync_ПереноситВсеПоля()
    {
        var input = NewInput(genre: "Роман", year: 1967, rating: 5, description: "Описание", isRead: true);

        var created = await CreateService().CreateAsync(input);

        Assert.Equal("Роман", created.Genre);
        Assert.Equal(1967, created.Year);
        Assert.Equal(5, created.Rating);
        Assert.Equal("Описание", created.Description);
        Assert.True(created.IsRead);
    }

    // ---- UpdateAsync ----

    [Fact]
    public async Task UpdateAsync_МеняетПоляИНеПлодитЗаписи()
    {
        var created = await CreateService().CreateAsync(NewInput());

        var updated = await CreateService().UpdateAsync(
            created.Id,
            NewInput(title: "Белая гвардия", author: "М. Булгаков", genre: "Роман", year: 1925, rating: 4, isRead: true));

        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Белая гвардия", updated.Title);
        Assert.Equal("М. Булгаков", updated.Author);
        Assert.Equal(1925, updated.Year);
        Assert.True(updated.IsRead);

        using var db = _database.CreateContext();
        Assert.Equal(1, db.Books.Count());
    }

    [Fact]
    public async Task UpdateAsync_МожетОчиститьНеобязательныеПоля()
    {
        var created = await CreateService().CreateAsync(
            NewInput(genre: "Роман", year: 1967, rating: 5, description: "Описание"));

        var updated = await CreateService().UpdateAsync(
            created.Id,
            NewInput(genre: null, year: null, rating: null, description: null));

        Assert.NotNull(updated);
        Assert.Null(updated.Genre);
        Assert.Null(updated.Year);
        Assert.Null(updated.Rating);
        Assert.Null(updated.Description);
    }

    [Fact]
    public async Task UpdateAsync_НеСбрасываетCreatedAt()
    {
        var created = await CreateService().CreateAsync(NewInput());

        var updated = await CreateService().UpdateAsync(created.Id, NewInput(title: "Другое"));

        Assert.NotNull(updated);
        Assert.Equal(created.CreatedAt, updated.CreatedAt);
    }

    [Fact]
    public async Task UpdateAsync_КнигиНет_Null()
    {
        var updated = await CreateService().UpdateAsync(404, NewInput());

        Assert.Null(updated);
    }

    // ---- DeleteAsync ----

    [Fact]
    public async Task DeleteAsync_КнигаЕсть_УдаляетИВозвращаетTrue()
    {
        var created = await CreateService().CreateAsync(NewInput());

        var deleted = await CreateService().DeleteAsync(created.Id);

        Assert.True(deleted);

        using var db = _database.CreateContext();
        Assert.Empty(db.Books);
    }

    [Fact]
    public async Task DeleteAsync_КнигиНет_False()
    {
        var deleted = await CreateService().DeleteAsync(404);

        Assert.False(deleted);
    }

    [Fact]
    public async Task DeleteAsync_УдаляетТолькоОднуКнигу()
    {
        SeedThreeBooks();
        var all = await CreateService().GetAllAsync(null, null);

        await CreateService().DeleteAsync(all[0].Id);

        var left = await CreateService().GetAllAsync(null, null);
        Assert.Equal(2, left.Count);
        Assert.DoesNotContain(all[0].Id, left.Select(b => b.Id));
    }

    // ---- GetGenresAsync ----

    [Fact]
    public async Task GetGenresAsync_ПустаяБаза_ПустойСписок()
    {
        var genres = await CreateService().GetGenresAsync();

        Assert.Empty(genres);
    }

    [Fact]
    public async Task GetGenresAsync_БезПовторовИПоАлфавиту()
    {
        using (var db = _database.CreateContext())
        {
            db.Books.AddRange(
                new Book { Title = "Раз", Author = "Автор", Genre = "Роман", CreatedAt = DateTime.UtcNow },
                new Book { Title = "Два", Author = "Автор", Genre = "Научпоп", CreatedAt = DateTime.UtcNow },
                new Book { Title = "Три", Author = "Автор", Genre = "Роман", CreatedAt = DateTime.UtcNow });
            db.SaveChanges();
        }

        var genres = await CreateService().GetGenresAsync();

        Assert.Equal(new[] { "Научпоп", "Роман" }, genres);
    }

    [Fact]
    public async Task GetGenresAsync_ПропускаетКнигиБезЖанра()
    {
        using (var db = _database.CreateContext())
        {
            db.Books.AddRange(
                new Book { Title = "Раз", Author = "Автор", Genre = null, CreatedAt = DateTime.UtcNow },
                new Book { Title = "Два", Author = "Автор", Genre = "", CreatedAt = DateTime.UtcNow },
                new Book { Title = "Три", Author = "Автор", Genre = "Роман", CreatedAt = DateTime.UtcNow });
            db.SaveChanges();
        }

        var genres = await CreateService().GetGenresAsync();

        Assert.Equal(new[] { "Роман" }, genres);
    }

    private static BookInput NewInput(
        string? title = "Мастер и Маргарита",
        string? author = "Михаил Булгаков",
        string? genre = null,
        int? year = null,
        int? rating = null,
        string? description = null,
        bool isRead = false,
        int? pages = null) =>
        new(title, author, genre, year, rating, description, isRead, pages);
}

using System.Net;
using System.Net.Http.Json;
using BookCatalog.Api.Dtos;
using Xunit;

namespace BookCatalog.Api.Tests.Integration;

/// <summary>
/// Проверки через HTTP по запущенному приложению: маршруты, коды ответов,
/// формат JSON — то, что модульные тесты сервиса увидеть не могут.
///
/// База настоящая и не пустая, поэтому тесты не рассчитывают на конкретное
/// содержимое каталога: создают книги с уникальными названиями, проверяют
/// утверждения относительно них и удаляют за собой в конце.
/// </summary>
[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public class BooksApiTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly List<int> _created = [];

    public BooksApiTests(RunningApiFixture api) => _client = api.Client;

    public Task InitializeAsync() => Task.CompletedTask;

    /// <summary>Убираем за собой: приложение работает с реальной базой.</summary>
    public async Task DisposeAsync()
    {
        foreach (var id in _created)
        {
            using var _ = await _client.DeleteAsync($"/api/books/{id}");
        }
    }

    // ---- Список и контракт ----

    [Fact]
    public async Task GET_список_ОтвечаетOk()
    {
        using var response = await _client.GetAsync("/api/books");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GET_список_ПоляВcamelCase()
    {
        await CreateBookAsync(Payload());

        using var response = await _client.GetAsync("/api/books");
        var json = await response.Content.ReadAsStringAsync();

        // На это имя полей завязан клиент: PascalCase сломал бы его молча.
        Assert.Contains("\"title\"", json);
        Assert.Contains("\"isRead\"", json);
        Assert.DoesNotContain("\"Title\"", json);
    }

    [Fact]
    public async Task GET_книгаПоНечисловомуId_ОтвечаетNotFound()
    {
        using var response = await _client.GetAsync("/api/books/не-число");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Создание ----

    [Fact]
    public async Task POST_КорректнаяКнига_ОтвечаетCreatedИОтдаётЕёПоLocation()
    {
        var payload = Payload(genre: "Роман", year: 1967, rating: 5, pages: 544, isRead: true);

        var created = await CreateBookAsync(payload);

        Assert.True(created.Id > 0);
        Assert.Equal(544, created.Pages);
        Assert.True(created.IsRead);

        using var byLocation = await _client.GetAsync($"/api/books/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, byLocation.StatusCode);

        var fetched = await byLocation.Content.ReadFromJsonAsync<BookDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Title, fetched.Title);
        Assert.Equal(created.Author, fetched.Author);
        Assert.Equal(created.Pages, fetched.Pages);
    }

    [Fact]
    public async Task POST_ЗаголовокLocationУказываетНаСозданнуюКнигу()
    {
        using var response = await _client.PostAsJsonAsync("/api/books", Payload());
        var created = await response.Content.ReadFromJsonAsync<BookDto>();
        Assert.NotNull(created);
        _created.Add(created.Id);

        Assert.Equal($"/api/books/{created.Id}", response.Headers.Location?.ToString());
    }

    [Theory]
    [InlineData("", "Title")]
    [InlineData("   ", "Title")]
    public async Task POST_ПустоеНазвание_ОтвечаетBadRequest(string title, string field)
    {
        using var response = await _client.PostAsJsonAsync("/api/books", Payload(title: title));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.NotNull(problem?.Errors);
        Assert.Contains(field, problem.Errors.Keys);
    }

    [Fact]
    public async Task POST_ОценкаВнеДиапазона_ОтвечаетBadRequest()
    {
        using var response = await _client.PostAsJsonAsync("/api/books", Payload(rating: 6));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.NotNull(problem?.Errors);
        Assert.Contains("Rating", problem.Errors.Keys);
    }

    [Fact]
    public async Task POST_СтраницыВнеДиапазона_ОтвечаетBadRequest()
    {
        using var response = await _client.PostAsJsonAsync("/api/books", Payload(pages: 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.NotNull(problem?.Errors);
        Assert.Contains("Pages", problem.Errors.Keys);
    }

    [Fact]
    public async Task POST_ДубльПоНазваниюИАвтору_ОтвечаетConflict()
    {
        var created = await CreateBookAsync(Payload());

        using var response = await _client.PostAsJsonAsync(
            "/api/books",
            Payload(title: created.Title, author: created.Author));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.False(string.IsNullOrWhiteSpace(problem?.Detail));
    }

    // ---- Изменение ----

    [Fact]
    public async Task PUT_БезСменыНазвания_ОтвечаетOkАНеConflict()
    {
        var created = await CreateBookAsync(Payload(year: 1967));

        using var response = await _client.PutAsJsonAsync(
            $"/api/books/{created.Id}",
            Payload(title: created.Title, author: created.Author, year: 1970));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<BookDto>();
        Assert.NotNull(updated);
        Assert.Equal(1970, updated.Year);
    }

    [Fact]
    public async Task PUT_ПереименованиеВЗанятуюПару_ОтвечаетConflict()
    {
        var first = await CreateBookAsync(Payload());
        var second = await CreateBookAsync(Payload());

        using var response = await _client.PutAsJsonAsync(
            $"/api/books/{second.Id}",
            Payload(title: first.Title, author: first.Author));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PUT_НесуществующаяКнига_ОтвечаетNotFound()
    {
        using var response = await _client.PutAsJsonAsync("/api/books/999999999", Payload());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Удаление ----

    [Fact]
    public async Task DELETE_СуществующаяКнига_ОтвечаетNoContentИБольшеНеНаходится()
    {
        var created = await CreateBookAsync(Payload());

        using var deleted = await _client.DeleteAsync($"/api/books/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        using var afterDelete = await _client.GetAsync($"/api/books/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task DELETE_НесуществующаяКнига_ОтвечаетNotFound()
    {
        using var response = await _client.DeleteAsync("/api/books/999999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Поиск, фильтр, сортировка ----

    [Fact]
    public async Task GET_ПоискКириллицейВДругомРегистре_НаходитКнигу()
    {
        var created = await CreateBookAsync(Payload(author: "Проверочный Автор " + Guid.NewGuid().ToString("N")));

        var found = await GetBooksAsync($"?search={Uri.EscapeDataString(created.Author.ToLowerInvariant())}");

        Assert.Contains(found, b => b.Id == created.Id);
    }

    [Fact]
    public async Task GET_ФильтрПоЖанру_ВозвращаетТолькоЭтотЖанр()
    {
        var genre = "Жанр-" + Guid.NewGuid().ToString("N");
        var created = await CreateBookAsync(Payload(genre: genre));

        var found = await GetBooksAsync($"?genre={Uri.EscapeDataString(genre)}");

        Assert.All(found, b => Assert.Equal(genre, b.Genre));
        Assert.Contains(found, b => b.Id == created.Id);
    }

    [Fact]
    public async Task GET_СортировкаПоГоду_ГодыНеУбываютАКнигиБезГодаВКонце()
    {
        await CreateBookAsync(Payload(year: 1800));
        await CreateBookAsync(Payload(year: null));

        var found = await GetBooksAsync("?sort=year");

        var years = found.Select(b => b.Year).ToList();
        var withYear = years.TakeWhile(y => y is not null).ToList();

        Assert.All(years.Skip(withYear.Count), y => Assert.Null(y));
        for (var i = 1; i < withYear.Count; i++)
        {
            Assert.True(withYear[i - 1] <= withYear[i], $"Год {withYear[i]} оказался после {withYear[i - 1]}");
        }
    }

    [Fact]
    public async Task GET_НеизвестнаяСортировка_ОтвечаетBadRequest()
    {
        using var response = await _client.GetAsync("/api/books?sort=страницы");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.NotNull(problem?.Errors);
        Assert.Contains("sort", problem.Errors.Keys);
    }

    [Fact]
    public async Task GET_Жанры_СодержатЖанрСозданнойКнигиБезПовторов()
    {
        var genre = "Жанр-" + Guid.NewGuid().ToString("N");
        await CreateBookAsync(Payload(genre: genre));
        await CreateBookAsync(Payload(genre: genre));

        using var response = await _client.GetAsync("/api/genres");
        var genres = await response.Content.ReadFromJsonAsync<List<string>>();

        Assert.NotNull(genres);
        Assert.Contains(genre, genres);
        Assert.Equal(genres.Count, genres.Distinct().Count());
    }

    // ---- Вспомогательное ----

    private async Task<BookDto> CreateBookAsync(object payload)
    {
        using var response = await _client.PostAsJsonAsync("/api/books", payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<BookDto>();
        Assert.NotNull(created);

        _created.Add(created.Id);
        return created;
    }

    private async Task<List<BookDto>> GetBooksAsync(string query)
    {
        using var response = await _client.GetAsync($"/api/books{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var books = await response.Content.ReadFromJsonAsync<List<BookDto>>();
        Assert.NotNull(books);
        return books;
    }

    // ---- Пограничные тесты ----

    [Fact]
    public async Task POST_КорректнаяКнига_ПограничСтраниц()
    {
        var payload = Payload(genre: "Роман", year: 1967, rating: 5, pages: 10000, isRead: true);

        var created = await CreateBookAsync(payload);

        Assert.True(created.Id > 0);
        Assert.Equal(10000, created.Pages);
        Assert.True(created.IsRead);

        using var byLocation = await _client.GetAsync($"/api/books/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, byLocation.StatusCode);

        var fetched = await byLocation.Content.ReadFromJsonAsync<BookDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Title, fetched.Title);
        Assert.Equal(created.Author, fetched.Author);
        Assert.Equal(created.Pages, fetched.Pages);
    }

    [Fact]
    public async Task POST_КорректнаяКнига_ПограничСтраниц_2()
    {
        var payload = Payload(genre: "Роман", year: 1967, rating: 5, pages: 1, isRead: true);

        var created = await CreateBookAsync(payload);

        Assert.True(created.Id > 0);
        Assert.Equal(1, created.Pages);
        Assert.True(created.IsRead);

        using var byLocation = await _client.GetAsync($"/api/books/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, byLocation.StatusCode);

        var fetched = await byLocation.Content.ReadFromJsonAsync<BookDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Title, fetched.Title);
        Assert.Equal(created.Author, fetched.Author);
        Assert.Equal(created.Pages, fetched.Pages);
    }


    [Fact]
    public async Task POST_КорректнаяКнига_ПограничЖанр()
    {
        var payload = Payload(genre: "Антиутопия, технологическое будущее, социальная изоляция, Мистический детектив, драма, романтический", year: 1967, rating: 5, pages: 544, isRead: true);

        var created = await CreateBookAsync(payload);

        Assert.True(created.Id > 0);
        Assert.Equal(544, created.Pages);
        Assert.True(created.IsRead);

        using var byLocation = await _client.GetAsync($"/api/books/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, byLocation.StatusCode);

        var fetched = await byLocation.Content.ReadFromJsonAsync<BookDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Title, fetched.Title);
        Assert.Equal(created.Author, fetched.Author);
        Assert.Equal(created.Pages, fetched.Pages);
    }

    [Fact]
    public async Task POST_КорректнаяКнига_ПограничГод()
    {
        var payload = Payload(genre: "Роман", year: 2027, rating: 5, pages: 544, isRead: false);

        var created = await CreateBookAsync(payload);

        Assert.True(created.Id > 0);
        Assert.Equal(544, created.Pages);

        using var byLocation = await _client.GetAsync($"/api/books/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, byLocation.StatusCode);

        var fetched = await byLocation.Content.ReadFromJsonAsync<BookDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Title, fetched.Title);
        Assert.Equal(created.Author, fetched.Author);
        Assert.Equal(created.Pages, fetched.Pages);
    }

    [Fact]
    public async Task POST_КорректнаяКнига_ПограничГод_2()
    {
        var payload = Payload(genre: "Роман", year: 1, rating: 5, pages: 544, isRead: false);

        var created = await CreateBookAsync(payload);

        Assert.True(created.Id > 0);
        Assert.Equal(544, created.Pages);

        using var byLocation = await _client.GetAsync($"/api/books/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, byLocation.StatusCode);

        var fetched = await byLocation.Content.ReadFromJsonAsync<BookDto>();
        Assert.NotNull(fetched);
        Assert.Equal(created.Title, fetched.Title);
        Assert.Equal(created.Author, fetched.Author);
        Assert.Equal(created.Pages, fetched.Pages);
    }

    /// <summary>
    /// Тело запроса собираем анонимным объектом, а не BookInput: тесты проверяют
    /// JSON-контракт и не должны ломаться от перестановки полей в record.
    /// Название и автор по умолчанию уникальны — в базе уже есть чужие книги.
    /// </summary>
    private static object Payload(
        string? title = null,
        string? author = null,
        string? genre = null,
        int? year = null,
        int? rating = null,
        string? description = null,
        bool isRead = false,
        int? pages = null) =>
        new
        {
            title = title ?? $"Тестовая книга {Guid.NewGuid():N}",
            author = author ?? $"Тестовый автор {Guid.NewGuid():N}",
            genre,
            year,
            rating,
            description,
            isRead,
            pages,
        };

    /// <summary>Ответ ProblemDetails — описан здесь, чтобы тесты не зависели от типов ASP.NET.</summary>
    private sealed record ProblemResponse(
        string? Title,
        string? Detail,
        Dictionary<string, string[]>? Errors);
}

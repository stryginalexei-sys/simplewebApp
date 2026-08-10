using System.Net.Http.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace BookCatalog.Api.Tests.Ui;

/// <summary>
/// Пример UI-тестов через Selenium: работа с формой и списком в браузере.
/// Проверяют то, что не видно на уровне HTTP — что данные доходят до экрана.
///
/// Каталог общий и не пустой, поэтому книги создаются с уникальными названиями,
/// а утверждения делаются про них, а не про весь список.
/// </summary>
[Collection(UiCollection.Name)]
[Trait("Category", "Ui")]
public class CatalogUiTests : IAsyncLifetime
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string _uiUrl;
    private readonly HttpClient _api;
    private readonly List<string> _createdTitles = [];

    public CatalogUiTests(BrowserFixture browser)
    {
        _driver = browser.Driver;
        _uiUrl = browser.BaseUrl;
        _wait = new WebDriverWait(_driver, Timeout);
        // React перерисовывает список во время ожидания, и найденный элемент устаревает.
        _wait.IgnoreExceptionTypes(typeof(StaleElementReferenceException));

        // Vite проксирует /api на бэкенд, поэтому убирать за собой можно
        // по тому же адресу, что открыт в браузере.
        _api = new HttpClient { BaseAddress = new Uri(_uiUrl) };
    }

    public Task InitializeAsync()
    {
        _driver.Navigate().GoToUrl(_uiUrl);
        _wait.Until(d => d.FindElements(By.CssSelector("form.form")).Count > 0);
        return Task.CompletedTask;
    }

    /// <summary>Удаляем созданное через API: UI для уборки слишком хрупок.</summary>
    public async Task DisposeAsync()
    {
        foreach (var title in _createdTitles)
        {
            var found = await _api.GetFromJsonAsync<List<BookRow>>(
                $"/api/books?search={Uri.EscapeDataString(title)}");

            foreach (var book in found ?? [])
            {
                using var _ = await _api.DeleteAsync($"/api/books/{book.Id}");
            }
        }

        _api.Dispose();
    }

    [Fact]
    public void СтраницаОткрывается_ВиденЗаголовокИСписок()
    {
        var heading = _driver.FindElement(By.CssSelector(".page-header h1"));

        Assert.Equal("Каталог книг", heading.Text);
        Assert.NotEmpty(_driver.FindElements(By.CssSelector(".toolbar input.search")));
    }

    [Fact]
    public void ДобавлениеКниги_КарточкаПоявляетсяВСписке()
    {
        var title = UniqueTitle();

        AddBook(title, author: "Тестовый автор", genre: "Проверка", year: "1999", pages: "123");

        var card = WaitForCard(title);

        Assert.Contains("Тестовый автор", card.Text);
        Assert.Contains("Проверка", card.Text);
        Assert.Contains("123 страниц", card.Text);
    }

    [Fact]
    public void ПоискПоНазванию_ОставляетТолькоНайденное()
    {
        var title = UniqueTitle();
        AddBook(title, author: "Автор для поиска");
        WaitForCard(title);

        Search(title);

        _wait.Until(_ => Cards().Count == 1);
        Assert.Contains(title, Cards()[0].Text);
    }

    [Fact]
    public void ПоискБезСовпадений_ПоказываетСообщениеОПустомРезультате()
    {
        Search("такой книги точно нет " + Guid.NewGuid().ToString("N"));

        var empty = _wait.Until(d => d.FindElements(By.CssSelector(".empty")).FirstOrDefault());

        Assert.NotNull(empty);
        Assert.Contains("Ничего не найдено", empty.Text);
    }

    [Fact]
    public void СортировкаПоНазванию_КарточкиИдутПоАлфавиту()
    {
        var sort = _driver.FindElement(By.CssSelector(".toolbar select[aria-label='Сортировка']"));
        new SelectElement(sort).SelectByValue("title");

        // Ждём, пока список перезапросится: до этого на экране прежний порядок.
        _wait.Until(_ => IsSortedByTitle());

        Assert.True(IsSortedByTitle());
    }

    [Fact]
    public void УдалениеКниги_КарточкаИсчезает()
    {
        var title = UniqueTitle();
        AddBook(title, author: "Автор на удаление");
        var card = WaitForCard(title);

        card.FindElement(By.CssSelector(".book-actions button.danger")).Click();
        _driver.SwitchTo().Alert().Accept();

        _wait.Until(_ => FindCard(title) is null);
        Assert.Null(FindCard(title));
    }

    // ---- Действия ----

    private void AddBook(
        string title,
        string author,
        string? genre = null,
        string? year = null,
        string? pages = null)
    {
        _createdTitles.Add(title);

        Fill("title", title);
        Fill("author", author);
        if (genre is not null) Fill("genre", genre);
        if (year is not null) Fill("year", year);
        if (pages is not null) Fill("pages", pages);

        _driver.FindElement(By.CssSelector("form.form button[type='submit']")).Click();
    }

    private void Fill(string name, string value)
    {
        var field = _driver.FindElement(By.CssSelector($"form.form [name='{name}']"));
        field.Clear();
        field.SendKeys(value);
    }

    private void Search(string term)
    {
        var search = _driver.FindElement(By.CssSelector(".toolbar input.search"));
        search.Clear();
        search.SendKeys(term);
    }

    // ---- Ожидания и поиск на странице ----

    private IReadOnlyList<IWebElement> Cards() =>
        _driver.FindElements(By.CssSelector(".book-list .book"));

    private IWebElement? FindCard(string title) =>
        Cards().FirstOrDefault(card =>
            card.FindElements(By.CssSelector("h3")).Any(h => h.Text == title));

    private IWebElement WaitForCard(string title) =>
        _wait.Until(_ => FindCard(title))
        ?? throw new InvalidOperationException($"Карточка «{title}» не появилась за {Timeout.TotalSeconds} с.");

    private bool IsSortedByTitle()
    {
        var titles = Cards()
            .Select(card => card.FindElement(By.CssSelector("h3")).Text)
            .ToList();

        return titles
            .Zip(titles.Skip(1), (previous, next) => string.CompareOrdinal(previous, next) <= 0)
            .All(ordered => ordered);
    }

    private static string UniqueTitle() => $"UI-тест {Guid.NewGuid():N}";

    /// <summary>Минимум полей для уборки — весь BookDto здесь не нужен.</summary>
    private sealed record BookRow(int Id, string Title);
}

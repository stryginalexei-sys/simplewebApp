using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;

namespace BookCatalog.Api.Tests.Ui;

/// <summary>
/// Один браузер на все UI-тесты. Приложение тесты не поднимают: должны работать
/// и API (порт 5187), и клиент (`npm run dev`, порт 5173).
///
/// Адрес клиента — переменная BOOKCATALOG_UI_URL.
/// BOOKCATALOG_UI_HEADED=1 запускает Chrome с окном, чтобы посмотреть глазами.
/// </summary>
public sealed class BrowserFixture : IAsyncLifetime
{
    public const string UiUrlVariable = "BOOKCATALOG_UI_URL";
    public const string HeadedVariable = "BOOKCATALOG_UI_HEADED";
    public const string DefaultUiUrl = "http://localhost:5173";

    public string BaseUrl { get; } =
        Environment.GetEnvironmentVariable(UiUrlVariable) ?? DefaultUiUrl;

    public IWebDriver Driver { get; private set; } = null!;

    public Task InitializeAsync()
    {
        var options = new ChromeOptions();

        if (Environment.GetEnvironmentVariable(HeadedVariable) != "1")
        {
            options.AddArgument("--headless=new");
        }

        options.AddArgument("--window-size=1280,1024");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");

        try
        {
            // Драйвер под установленный Chrome Selenium Manager скачивает сам,
            // отдельный пакет с chromedriver не нужен.
            Driver = new ChromeDriver(options);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException(
                "Не удалось запустить Chrome. Нужен установленный Google Chrome; " +
                "драйвер Selenium подберёт сам.",
                e);
        }

        // Неявных ожиданий не используем: они плохо уживаются с явными
        // и превращают падение в долгое непонятное зависание.
        Driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Driver?.Quit();
        return Task.CompletedTask;
    }
}

[CollectionDefinition(Name)]
public sealed class UiCollection : ICollectionFixture<BrowserFixture>
{
    public const string Name = "Браузер";
}

using Xunit;

namespace BookCatalog.Api.Tests.Integration;

/// <summary>
/// Подключается к уже запущенному приложению. Тесты не поднимают сервер сами —
/// он должен работать отдельно (`dotnet run` в server/BookCatalog.Api).
/// Адрес берётся из переменной окружения BOOKCATALOG_API_URL, по умолчанию localhost:5187.
/// </summary>
public sealed class RunningApiFixture : IAsyncLifetime
{
    public const string BaseUrlVariable = "BOOKCATALOG_API_URL";
    public const string DefaultBaseUrl = "http://localhost:5187";

    public string BaseUrl { get; } =
        Environment.GetEnvironmentVariable(BaseUrlVariable) ?? DefaultBaseUrl;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Client = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(10),
        };

        // Проверяем доступность один раз на весь прогон: иначе каждый тест
        // упал бы с невнятным «Connection refused» вместо понятной причины.
        try
        {
            using var response = await Client.GetAsync("/api/books");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            throw new InvalidOperationException(
                $"Приложение на {BaseUrl} не отвечает. Запустите его: " +
                $"cd server/BookCatalog.Api && dotnet run. " +
                $"Другой адрес задаётся переменной {BaseUrlVariable}.",
                e);
        }
    }

    public Task DisposeAsync()
    {
        Client?.Dispose();
        return Task.CompletedTask;
    }
}

/// <summary>
/// Одно подключение на все интеграционные тесты — приложение поднимается не тестами,
/// поэтому проверять его доступность повторно смысла нет.
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<RunningApiFixture>
{
    public const string Name = "Запущенное приложение";
}

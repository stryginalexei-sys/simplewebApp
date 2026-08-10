# Каталог книг

Простое веб-приложение: список книг с поиском, фильтром по жанру и CRUD.

- **Бэкенд** — ASP.NET Core Minimal API (C#), EF Core + SQLite
- **Фронтенд** — React 19 + TypeScript на Vite
- **База** — локальный файл `books.db`, создаётся при первом запуске и наполняется примерами

## Структура

```
BookCatalog.sln
server/BookCatalog.Api/        API
├── Program.cs                 эндпоинты и запуск
├── Services/BookService.cs    логика каталога: поиск, фильтр, CRUD
├── Models/Book.cs             сущность книги
├── Dtos/BookDtos.cs           входные/выходные модели
├── Data/AppDbContext.cs       контекст EF Core
├── Data/DbSeeder.cs           стартовые данные
└── Validation/                проверка полей
tests/BookCatalog.Api.Tests/   тесты (xUnit)
├── BookServiceTests.cs        поиск, фильтр, CRUD
├── BookInputValidatorTests.cs правила валидации
├── DbSeederTests.cs           стартовые данные
└── TestDatabase.cs            SQLite в памяти для тестов
client/                        React-приложение
├── src/App.tsx                состояние и сборка страницы
├── src/api.ts                 обёртка над fetch
├── src/types.ts               типы, общие с API
└── src/components/            BookForm, BookList, Toolbar
```

## Что нужно установить

На этой машине ничего не устанавливалось — код написан, но **не собирался и не запускался**.
Чтобы запустить, понадобятся:

- .NET SDK 10 ([dotnet.microsoft.com](https://dotnet.microsoft.com/download))
- Node.js 20+ ([nodejs.org](https://nodejs.org))

Если поставите .NET 9, поменяйте в `server/BookCatalog.Api/BookCatalog.Api.csproj`
`<TargetFramework>net10.0</TargetFramework>` на `net9.0`, а версию пакета
`Microsoft.EntityFrameworkCore.Sqlite` — на `9.0.0`.

## Запуск

В двух терминалах.

**1. API** (порт 5187):

```bash
cd server/BookCatalog.Api
dotnet run
```

**2. Клиент** (порт 5173):

```bash
cd client
npm install
npm run dev
```

Открыть http://localhost:5173. Vite проксирует `/api` на бэкенд, так что CORS в разработке не задействован.

## Тесты

Всё лежит в одном проекте `tests/BookCatalog.Api.Tests`, но делится на три вида —
они различаются трейтом `Category` и требованиями к окружению.

### Модульные — ничего запускать не нужно

```bash
dotnet test --filter "Category!=Integration&Category!=Ui"
```

`BookService` (поиск, фильтр, сортировка, дубли, CRUD), `BookInputValidator` и `DbSeeder`.
Каждый тест работает с настоящим SQLite в памяти (`Data Source=:memory:`), а не с
InMemory-провайдером EF — так запросы реально транслируются в SQL. База создаётся заново
на каждый тест, общего состояния нет.

### Интеграционные — по запущенному API

Сначала в отдельном терминале поднимите API (`cd server/BookCatalog.Api && dotnet run`), затем:

```bash
dotnet test --filter "Category=Integration"
```

Это чёрный ящик поверх HTTP: маршруты, коды ответов, формат JSON, `409` на дублях. Тесты
работают с настоящей базой, поэтому создают книги с уникальными названиями, проверяют
утверждения относительно них и удаляют за собой. На конкретное содержимое каталога они не
рассчитывают — иначе падали бы от чужих данных.

Адрес меняется переменной `BOOKCATALOG_API_URL` (по умолчанию `http://localhost:5187`).

### UI — по запущенному приложению целиком

Нужны поднятые API и клиент (`npm run dev`), а также установленный Google Chrome —
драйвер Selenium Manager скачает сам.

```bash
dotnet test --filter "Category=Ui"
```

Проверяют форму, список, поиск, сортировку и удаление в браузере. По умолчанию Chrome
работает без окна; `BOOKCATALOG_UI_HEADED=1` покажет происходящее на экране, что удобно при
разборе падений. Адрес клиента — `BOOKCATALOG_UI_URL` (по умолчанию `http://localhost:5173`).

Селекторы опираются на атрибуты `name` у полей формы и классы `.book`, `.toolbar` —
не переименовывайте их, не поправив тесты.

Тестов на React-часть в изоляции нет — если понадобятся, добавим Vitest + Testing Library.

## API

| Метод    | Путь                                        | Описание                        |
| -------- | ------------------------------------------- | ------------------------------- |
| `GET`    | `/api/books?search=...&genre=...&sort=...`  | Список книг, поиск и фильтр     |
| `GET`    | `/api/books/{id}`                           | Одна книга                      |
| `POST`   | `/api/books`                                | Создать                         |
| `PUT`    | `/api/books/{id}`                           | Обновить                        |
| `DELETE` | `/api/books/{id}`                           | Удалить                         |
| `GET`    | `/api/genres`                               | Жанры, встречающиеся в каталоге |

`sort` принимает `author` (по умолчанию), `title`, `year` или `rating`; книги без года
и без оценки уходят в конец списка. Неизвестное значение — `400`, а не молчаливый откат
к сортировке по умолчанию.

Поля книги: `title` и `author` обязательны, `genre`, `year`, `rating` (1–5), `pages` (1–10000),
`description`, `isRead` — нет. Ошибки валидации возвращаются как `ProblemDetails` с полем
`errors` и показываются под соответствующими полями формы.

Пара «название + автор» уникальна: `POST` и `PUT` с уже занятой парой отвечают `409` и
`ProblemDetails` с текстом в `detail`. Сравнение точное и с учётом регистра — SQLite без ICU
не умеет иначе для кириллицы.

## Заметки по реализации

- **Миграций нет.** Схема создаётся через `EnsureCreated()` — для локального приложения этого
  достаточно и не нужен `dotnet-ef`. Если добавите поля в `Book`, удалите `books.db`,
  либо переходите на миграции.
- **Поиск идёт в памяти.** `LIKE` и `lower()` в SQLite без ICU регистронезависимы только для
  латиницы, поэтому фильтрация по строке делается в C# через `OrdinalIgnoreCase` — это корректно
  работает с кириллицей. Фильтр по жанру и сортировка остаются в SQL. На больших объёмах это
  стоит заменить на FTS5.
- **Файл базы** лежит в рабочем каталоге запуска (`server/BookCatalog.Api/books.db`) и не
  коммитится — см. `.gitignore`.

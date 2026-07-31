using BookCatalog.Api.Data;
using BookCatalog.Api.Dtos;
using BookCatalog.Api.Models;
using BookCatalog.Api.Validation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=books.db"));

// Нужен только для запуска React-дева отдельно от API без Vite-прокси.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

// Локальная база: файл создаётся при первом запуске, миграции не нужны.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}

app.UseCors();

var books = app.MapGroup("/api/books");

// Список книг с поиском по названию/автору и фильтром по жанру.
books.MapGet("/", async (AppDbContext db, string? search, string? genre) =>
{
    var query = db.Books.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(genre))
    {
        var g = genre.Trim();
        query = query.Where(b => b.Genre == g);
    }

    var found = await query
        .OrderBy(b => b.Author)
        .ThenBy(b => b.Title)
        .ToListAsync();

    // Поиск делаем в памяти: LIKE и lower() в SQLite без ICU регистронезависимы
    // только для латиницы, а OrdinalIgnoreCase корректно работает и с кириллицей.
    // Для локального каталога на несколько тысяч книг этого достаточно.
    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.Trim();
        found = found
            .Where(b =>
                b.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                b.Author.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    return Results.Ok(found.Select(BookDto.From).ToList());
});

books.MapGet("/{id:int}", async (AppDbContext db, int id) =>
{
    var book = await db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
    return book is null ? Results.NotFound() : Results.Ok(BookDto.From(book));
});

books.MapPost("/", async (AppDbContext db, BookInput input) =>
{
    var errors = BookInputValidator.Validate(input);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var book = new Book
    {
        Title = input.Title!.Trim(),
        Author = input.Author!.Trim(),
        Genre = Normalize(input.Genre),
        Year = input.Year,
        Rating = input.Rating,
        Description = Normalize(input.Description),
        IsRead = input.IsRead,
        CreatedAt = DateTime.UtcNow,
    };

    db.Books.Add(book);
    await db.SaveChangesAsync();

    return Results.Created($"/api/books/{book.Id}", BookDto.From(book));
});

books.MapPut("/{id:int}", async (AppDbContext db, int id, BookInput input) =>
{
    var errors = BookInputValidator.Validate(input);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var book = await db.Books.FirstOrDefaultAsync(b => b.Id == id);
    if (book is null)
    {
        return Results.NotFound();
    }

    book.Title = input.Title!.Trim();
    book.Author = input.Author!.Trim();
    book.Genre = Normalize(input.Genre);
    book.Year = input.Year;
    book.Rating = input.Rating;
    book.Description = Normalize(input.Description);
    book.IsRead = input.IsRead;

    await db.SaveChangesAsync();

    return Results.Ok(BookDto.From(book));
});

books.MapDelete("/{id:int}", async (AppDbContext db, int id) =>
{
    var book = await db.Books.FirstOrDefaultAsync(b => b.Id == id);
    if (book is null)
    {
        return Results.NotFound();
    }

    db.Books.Remove(book);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

// Список жанров, которые уже есть в базе — для фильтра на клиенте.
app.MapGet("/api/genres", async (AppDbContext db) =>
{
    var genres = await db.Books.AsNoTracking()
        .Where(b => b.Genre != null && b.Genre != "")
        .Select(b => b.Genre!)
        .Distinct()
        .OrderBy(g => g)
        .ToListAsync();

    return Results.Ok(genres);
});

app.Run();

static string? Normalize(string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : value.Trim();

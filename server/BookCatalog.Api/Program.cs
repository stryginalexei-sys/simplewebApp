using BookCatalog.Api.Data;
using BookCatalog.Api.Dtos;
using BookCatalog.Api.Services;
using BookCatalog.Api.Validation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=books.db"));

builder.Services.AddScoped<BookService>();

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

books.MapGet("/", async (BookService service, string? search, string? genre) =>
    Results.Ok(await service.GetAllAsync(search, genre)));

books.MapGet("/{id:int}", async (BookService service, int id) =>
{
    var book = await service.GetByIdAsync(id);
    return book is null ? Results.NotFound() : Results.Ok(book);
});

books.MapPost("/", async (BookService service, BookInput input) =>
{
    var errors = BookInputValidator.Validate(input);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var created = await service.CreateAsync(input);

    return Results.Created($"/api/books/{created.Id}", created);
});

books.MapPut("/{id:int}", async (BookService service, int id, BookInput input) =>
{
    var errors = BookInputValidator.Validate(input);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }
    var conflict = BookInputValidator.ValidateConflict(service, input);
    if (conflict!= null)
    {
       return Results.Conflict(conflict);
    }
    
    var updated = await service.UpdateAsync(id, input);

    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

books.MapDelete("/{id:int}", async (BookService service, int id) =>
    await service.DeleteAsync(id) ? Results.NoContent() : Results.NotFound());

app.MapGet("/api/genres", async (BookService service) =>
    Results.Ok(await service.GetGenresAsync()));

app.Run();

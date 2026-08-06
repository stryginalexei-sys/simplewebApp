using BookCatalog.Api.Data;
using BookCatalog.Api.Dtos;
using BookCatalog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BookCatalog.Api.Services;

/// <summary>Вся работа с каталогом: чтение, поиск и изменение книг.</summary>
public class BookService(AppDbContext db)
{
    /// <summary>Значения параметра sort, которые понимает <see cref="GetAllAsync"/>.</summary>
    public static readonly string[] SortOptions = ["author", "title", "year", "rating"];

    /// <summary>Пустое значение допустимо — это сортировка по умолчанию (по автору).</summary>
    public static bool IsKnownSort(string? sort) =>
        string.IsNullOrWhiteSpace(sort) || SortOptions.Contains(sort.Trim());

    /// <summary>
    /// Книги с поиском, фильтром по жанру и сортировкой. Все параметры необязательны:
    /// пустые значения означают «без фильтра», пустой <paramref name="sort"/> — по автору.
    /// </summary>
    public async Task<List<BookDto>> GetAllAsync(string? search, string? genre, string? sort = null)
    {
        var query = db.Books.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(genre))
        {
            var g = genre.Trim();
            query = query.Where(b => b.Genre == g);
        }

        query = sort?.Trim() switch
        {
            "title" => query.OrderBy(b => b.Title),
            // Книги без года и без оценки уходят в конец списка, а не в начало.
            "year" => query.OrderBy(b => b.Year == null).ThenBy(b => b.Year).ThenBy(b => b.Title),
            "rating" => query.OrderByDescending(b => b.Rating).ThenBy(b => b.Title),
            _ => query.OrderBy(b => b.Author).ThenBy(b => b.Title),
        };

        var found = await query.ToListAsync();

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

        return found.Select(BookDto.From).ToList();
    }

    /// <summary>
    /// Есть ли в каталоге книга с таким названием и автором.
    /// <paramref name="exceptId"/> исключает саму редактируемую книгу — иначе сохранение
    /// без изменения названия считалось бы дублем самой себя.
    /// Сравнение точное: SQLite без ICU не умеет сравнивать кириллицу без учёта регистра.
    /// </summary>
    public async Task<bool> ExistsAsync(string? title, string? author, int? exceptId = null)
    {
        var t = title?.Trim() ?? string.Empty;
        var a = author?.Trim() ?? string.Empty;

        var query = db.Books.AsNoTracking().Where(b => b.Title == t && b.Author == a);

        if (exceptId is { } id)
        {
            query = query.Where(b => b.Id != id);
        }

        return await query.AnyAsync();
    }

    public async Task<BookDto?> GetByIdAsync(int id)
    {
        var book = await db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        return book is null ? null : BookDto.From(book);
    }

    public async Task<BookDto> CreateAsync(BookInput input)
    {
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
            Pages = input.Pages,
        };

        db.Books.Add(book);
        await db.SaveChangesAsync();

        return BookDto.From(book);
    }

    /// <summary>Обновляет книгу. Возвращает null, если книги с таким id нет.</summary>
    public async Task<BookDto?> UpdateAsync(int id, BookInput input)
    {
        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == id);
        if (book is null)
        {
            return null;
        }

        book.Title = input.Title!.Trim();
        book.Author = input.Author!.Trim();
        book.Genre = Normalize(input.Genre);
        book.Year = input.Year;
        book.Rating = input.Rating;
        book.Description = Normalize(input.Description);
        book.IsRead = input.IsRead;
        book.Pages = input.Pages;

        await db.SaveChangesAsync();

        return BookDto.From(book);
    }

    /// <summary>Удаляет книгу. Возвращает false, если книги с таким id нет.</summary>
    public async Task<bool> DeleteAsync(int id)
    {
        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == id);
        if (book is null)
        {
            return false;
        }

        db.Books.Remove(book);
        await db.SaveChangesAsync();

        return true;
    }

    /// <summary>Жанры, которые уже есть в каталоге — для фильтра на клиенте.</summary>
    public async Task<List<string>> GetGenresAsync() =>
        await db.Books.AsNoTracking()
            .Where(b => b.Genre != null && b.Genre != "")
            .Select(b => b.Genre!)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync();

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

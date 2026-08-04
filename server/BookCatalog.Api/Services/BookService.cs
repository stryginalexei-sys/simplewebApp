using BookCatalog.Api.Data;
using BookCatalog.Api.Dtos;
using BookCatalog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BookCatalog.Api.Services;

/// <summary>Вся работа с каталогом: чтение, поиск и изменение книг.</summary>
public class BookService(AppDbContext db)
{
    /// <summary>
    /// Книги, отсортированные по автору и названию.
    /// Пустые <paramref name="search"/> и <paramref name="genre"/> означают «без фильтра».
    /// </summary>
    public async Task<List<BookDto>> GetAllAsync(string? search, string? genre)
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

        return found.Select(BookDto.From).ToList();
    }

    public async Task<List<BookDto>> GetAllAsync()
    {
        var query = db.Books.AsNoTracking();

        var found = await query
            .OrderBy(b => b.Author)
            .ThenBy(b => b.Title)
            .ToListAsync();

        // Поиск делаем в памяти: LIKE и lower() в SQLite без ICU регистронезависимы
        // только для латиницы, а OrdinalIgnoreCase корректно работает и с кириллицей.
        // Для локального каталога на несколько тысяч книг этого достаточно.    
        return found.Select(BookDto.From).ToList();
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

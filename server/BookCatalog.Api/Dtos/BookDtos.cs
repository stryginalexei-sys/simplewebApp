using BookCatalog.Api.Models;

namespace BookCatalog.Api.Dtos;

/// <summary>Данные, которые приходят от клиента при создании/обновлении книги.</summary>
public record BookInput(
    string? Title,
    string? Author,
    string? Genre,
    int? Year,
    int? Rating,
    string? Description,
    bool IsRead,
    int? Pages

    );

/// <summary>Данные, которые уходят клиенту.</summary>
public record BookDto(
    int Id,
    string Title,
    string Author,
    string? Genre,
    int? Year,
    int? Rating,
    string? Description,
    bool IsRead,
    DateTime CreatedAt,
    int? Pages
    )
{
    public static BookDto From(Book book) => new(
        book.Id,
        book.Title,
        book.Author,
        book.Genre,
        book.Year,
        book.Rating,
        book.Description,
        book.IsRead,
        book.CreatedAt,
        book.Pages
        );
}

using BookCatalog.Api.Models;

namespace BookCatalog.Api.Data;

public static class DbSeeder
{
    /// <summary>Наполняет базу примерами, если она пустая.</summary>
    public static void Seed(AppDbContext db)
    {
        if (db.Books.Any())
        {
            return;
        }

        db.Books.AddRange(
            new Book
            {
                Title = "Мастер и Маргарита",
                Author = "Михаил Булгаков",
                Genre = "Роман",
                Year = 1967,
                Rating = 5,
                Description = "Дьявол приезжает в Москву 1930-х.",
                IsRead = true,
                CreatedAt = DateTime.UtcNow,
            },
            new Book
            {
                Title = "Чистый код",
                Author = "Роберт Мартин",
                Genre = "Программирование",
                Year = 2008,
                Rating = 4,
                Description = "О читаемости и поддерживаемости кода.",
                IsRead = true,
                CreatedAt = DateTime.UtcNow,
            },
            new Book
            {
                Title = "Пикник на обочине",
                Author = "Аркадий и Борис Стругацкие",
                Genre = "Фантастика",
                Year = 1972,
                Rating = 5,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
            },
            new Book
            {
                Title = "Краткая история времени",
                Author = "Стивен Хокинг",
                Genre = "Научпоп",
                Year = 1988,
                Rating = 4,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
            });

        db.SaveChanges();
    }
}

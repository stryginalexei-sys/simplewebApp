using BookCatalog.Api.Dtos;

namespace BookCatalog.Api.Validation;

public static class BookInputValidator
{
    /// <summary>
    /// Проверяет входные данные. Возвращает словарь ошибок в формате,
    /// который принимает Results.ValidationProblem (пустой — значит всё в порядке).
    /// </summary>
    public static Dictionary<string, string[]> Validate(BookInput input)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(input.Title))
        {
            errors[nameof(input.Title)] = ["Название обязательно."];
        }
        else if (input.Title.Trim().Length > 200)
        {
            errors[nameof(input.Title)] = ["Название не длиннее 200 символов."];
        }

        if (string.IsNullOrWhiteSpace(input.Author))
        {
            errors[nameof(input.Author)] = ["Автор обязателен."];
        }
        else if (input.Author.Trim().Length > 200)
        {
            errors[nameof(input.Author)] = ["Имя автора не длиннее 200 символов."];
        }

        if (input.Genre is not null && input.Genre.Trim().Length > 100)
        {
            errors[nameof(input.Genre)] = ["Жанр не длиннее 100 символов."];
        }

        if (input.Year is { } year && (year < 1 || year > DateTime.UtcNow.Year + 1))
        {
            errors[nameof(input.Year)] = [$"Год должен быть от 1 до {DateTime.UtcNow.Year + 1}."];
        }

        if (input.Rating is { } rating && (rating < 1 || rating > 5))
        {
            errors[nameof(input.Rating)] = ["Оценка должна быть от 1 до 5."];
        }

        if (input.Description is not null && input.Description.Trim().Length > 2000)
        {
            errors[nameof(input.Description)] = ["Описание не длиннее 2000 символов."];
        }

        return errors;
    }
}

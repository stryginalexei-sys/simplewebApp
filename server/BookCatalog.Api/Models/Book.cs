namespace BookCatalog.Api.Models;

public class Book
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public string? Genre { get; set; }

    public int? Year { get; set; }

    /// <summary>Оценка от 1 до 5, необязательная.</summary>
    public int? Rating { get; set; }

    public string? Description { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }
    
    public int? Pages { get; set; } 
}

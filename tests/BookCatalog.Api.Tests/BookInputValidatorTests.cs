using BookCatalog.Api.Dtos;
using BookCatalog.Api.Validation;
using Xunit;

namespace BookCatalog.Api.Tests;

public class BookInputValidatorTests
{
    private static BookInput Valid(
        string? title = "Мастер и Маргарита",
        string? author = "Михаил Булгаков",
        string? genre = "Роман",
        int? year = 1967,
        int? rating = 5,
        string? description = "Дьявол приезжает в Москву.",
        bool isRead = true,
        int? pages = 666
        ) =>
        new(title, author, genre, year, rating, description, isRead, pages);

    [Fact]
    public void КорректныеДанные_БезОшибок()
    {
        var errors = BookInputValidator.Validate(Valid());
        Assert.Empty(errors);
    }

    [Fact]
    public void НеобязательныеПоляПустые_БезОшибок()
    {
        var input = Valid(genre: null, year: null, rating: null, description: null, pages: null);

        var errors = BookInputValidator.Validate(input);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void НазваниеПустое_ОшибкаTitle(string? title)
    {
        var errors = BookInputValidator.Validate(Valid(title: title));

        Assert.Contains("Title", errors.Keys);
        Assert.Equal("Название обязательно.", errors["Title"][0]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void АвторПустой_ОшибкаAuthor(string? author)
    {
        var errors = BookInputValidator.Validate(Valid(author: author));

        Assert.Contains("Author", errors.Keys);
        Assert.Equal("Автор обязателен.", errors["Author"][0]);
    }

    [Fact]
    public void НазваниеИАвторПустые_ДвеОшибки()
    {
        var errors = BookInputValidator.Validate(Valid(title: "", author: ""));

        Assert.Equal(2, errors.Count);
        Assert.Contains("Title", errors.Keys);
        Assert.Contains("Author", errors.Keys);
    }

    [Fact]
    public void НазваниеРовно200Символов_БезОшибок()
    {
        var errors = BookInputValidator.Validate(Valid(title: new string('я', 200)));

        Assert.Empty(errors);
    }

    [Fact]
    public void НазваниеДлиннее200_ОшибкаTitle()
    {
        var errors = BookInputValidator.Validate(Valid(title: new string('я', 201)));

        Assert.Contains("Title", errors.Keys);
    }

    [Fact]
    public void ИмяАвтораДлиннее200_ОшибкаAuthor()
    {
        var errors = BookInputValidator.Validate(Valid(author: new string('я', 201)));

        Assert.Contains("Author", errors.Keys);
    }

    [Fact]
    public void ЖанрДлиннее100_ОшибкаGenre()
    {
        var errors = BookInputValidator.Validate(Valid(genre: new string('я', 101)));

        Assert.Contains("Genre", errors.Keys);
    }

    [Fact]
    public void ОписаниеДлиннее2000_ОшибкаDescription()
    {
        var errors = BookInputValidator.Validate(Valid(description: new string('я', 2001)));

        Assert.Contains("Description", errors.Keys);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void ГодВнеДиапазона_ОшибкаYear(int year)
    {
        var errors = BookInputValidator.Validate(Valid(year: year));

        Assert.Contains("Year", errors.Keys);
    }

    [Fact]
    public void ГодСлишкомДалекоВБудущем_ОшибкаYear()
    {
        var errors = BookInputValidator.Validate(Valid(year: DateTime.UtcNow.Year + 2));

        Assert.Contains("Year", errors.Keys);
    }

    [Fact]
    public void ГодСледующий_ДопустимДляАнонсов()
    {
        var errors = BookInputValidator.Validate(Valid(year: DateTime.UtcNow.Year + 1));

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void ОценкаВДиапазоне_БезОшибок(int rating)
    {
        var errors = BookInputValidator.Validate(Valid(rating: rating));

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void ОценкаВнеДиапазона_ОшибкаRating(int rating)
    {
        var errors = BookInputValidator.Validate(Valid(rating: rating));

        Assert.Contains("Rating", errors.Keys);
    }

    [Fact]
    public void ПробелыВокругЗначенийНеМешают_ДлинаСчитаетсяБезНих()
    {
        var input = Valid(title: $"  {new string('я', 200)}  ");

        var errors = BookInputValidator.Validate(input);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(500)]
    [InlineData(10000)]
    public void СтраницыВДиапазоне_БезОшибок(int pages)
    {
        var errors = BookInputValidator.Validate(Valid(pages: pages));
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10007)]
    [InlineData(-1)]
    public void СтраницыВнеДиапазона_ОшибкаPages(int pages)
    {
        var errors = BookInputValidator.Validate(Valid(pages: pages));

        Assert.Contains("Pages", errors.Keys);
    }

}

using BookCatalog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BookCatalog.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var book = modelBuilder.Entity<Book>();

        book.Property(b => b.Title).IsRequired().HasMaxLength(200);
        book.Property(b => b.Author).IsRequired().HasMaxLength(200);
        book.Property(b => b.Genre).HasMaxLength(100);
        book.Property(b => b.Description).HasMaxLength(2000);

        book.HasIndex(b => b.Title);
        book.HasIndex(b => b.Author);
    }
}

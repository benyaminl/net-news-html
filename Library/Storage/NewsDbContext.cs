using Microsoft.EntityFrameworkCore;
using net_news_html.Models;

namespace net_news_html.Library.Storage
{
    public class NewsDbContext : DbContext
    {
        public NewsDbContext(DbContextOptions<NewsDbContext> options) : base(options)
        {
        }

        public DbSet<SavedNewsItem> SavedNews { get; set; }
        public DbSet<PassKey> PassKeys { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SavedNewsItem>()
                .HasKey(n => n.Url);

            modelBuilder.Entity<SavedNewsItem>()
                .Property(n => n.Url)
                .IsRequired();

            modelBuilder.Entity<SavedNewsItem>()
                .Property(n => n.Title)
                .IsRequired();

            modelBuilder.Entity<SavedNewsItem>()
                .Property(n => n.SaveDate)
                .IsRequired();

            modelBuilder.Entity<SavedNewsItem>()
                .HasOne(s => s.PassKey)
                .WithMany()
                .HasForeignKey(s => s.PassKeyId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
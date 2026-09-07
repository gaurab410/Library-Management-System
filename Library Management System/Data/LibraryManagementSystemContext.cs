using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Models;

namespace LibraryManagementSystem.Data
{
    public class LibraryManagementSystemContext : DbContext
    {
        public LibraryManagementSystemContext(DbContextOptions<LibraryManagementSystemContext> options)
            : base(options)
        {
        }

        public DbSet<LibraryItem> LibraryItems { get; set; } = default!;
        public DbSet<BookItem> BookItems { get; set; } = default!;
        public DbSet<MusicItem> MusicItems { get; set; } = default!;
        public DbSet<ToyItem> ToyItems { get; set; } = default!;

        public DbSet<Borrower> Borrowers { get; set; } = default!;
        public DbSet<BorrowTransaction> BorrowTransactions { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Table-Per-Hierarchy (TPH) Inheritance Configuration for LibraryItem
            modelBuilder.Entity<LibraryItem>()
                .HasDiscriminator<string>("Discriminator")
                .HasValue<BookItem>("BookItem")
                .HasValue<MusicItem>("MusicItem")
                .HasValue<ToyItem>("ToyItem");

            // Unique index on LibraryCode
            modelBuilder.Entity<LibraryItem>()
                .HasIndex(i => i.LibraryCode)
                .IsUnique();

            // Unique index on Borrower CardNumber
            modelBuilder.Entity<Borrower>()
                .HasIndex(b => b.CardNumber)
                .IsUnique();

            // Configure DeleteBehavior to Restrict deletion of borrowed items and borrowers with transactions
            modelBuilder.Entity<BorrowTransaction>()
                .HasOne(t => t.LibraryItem)
                .WithMany(i => i.BorrowTransactions)
                .HasForeignKey(t => t.LibraryItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BorrowTransaction>()
                .HasOne(t => t.Borrower)
                .WithMany(b => b.BorrowTransactions)
                .HasForeignKey(t => t.BorrowerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

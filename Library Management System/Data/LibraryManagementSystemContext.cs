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
        public DbSet<Branch> Branches { get; set; } = default!;
        public DbSet<BranchTransfer> BranchTransfers { get; set; } = default!;
        public DbSet<Reservation> Reservations { get; set; } = default!;
        public DbSet<Notification> Notifications { get; set; } = default!;

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

            // Configure Branch -> LibraryItem (Restrict delete)
            modelBuilder.Entity<LibraryItem>()
                .HasOne(i => i.Branch)
                .WithMany(b => b.Items)
                .HasForeignKey(i => i.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Branch -> Borrower (HomeBranch)
            modelBuilder.Entity<Borrower>()
                .HasOne(b => b.HomeBranch)
                .WithMany()
                .HasForeignKey(b => b.HomeBranchId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Branch -> BorrowTransaction (Restrict delete)
            modelBuilder.Entity<BorrowTransaction>()
                .HasOne(t => t.Branch)
                .WithMany()
                .HasForeignKey(t => t.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure BranchTransfer relationships (Restrict delete)
            modelBuilder.Entity<BranchTransfer>()
                .HasOne(t => t.LibraryItem)
                .WithMany()
                .HasForeignKey(t => t.LibraryItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BranchTransfer>()
                .HasOne(t => t.FromBranch)
                .WithMany()
                .HasForeignKey(t => t.FromBranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BranchTransfer>()
                .HasOne(t => t.ToBranch)
                .WithMany()
                .HasForeignKey(t => t.ToBranchId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure DeleteBehavior to Restrict deletion of items and borrowers with reservations
            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.LibraryItem)
                .WithMany()
                .HasForeignKey(r => r.LibraryItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Borrower)
                .WithMany()
                .HasForeignKey(r => r.BorrowerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

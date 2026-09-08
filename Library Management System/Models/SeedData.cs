using Microsoft.EntityFrameworkCore;
using LibraryManagementSystem.Data;

namespace LibraryManagementSystem.Models
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new LibraryManagementSystemContext(
                serviceProvider.GetRequiredService<DbContextOptions<LibraryManagementSystemContext>>()))
            {
                // Ensure database tables are created
                context.Database.EnsureCreated();

                // If items already exist, seed is complete
                if (context.LibraryItems.Any())
                {
                    return;
                }

                // 1. Seed Books (at least 5)
                var books = new List<BookItem>
                {
                    new BookItem
                    {
                        LibraryCode = "BK-1001",
                        Name = "The Great Gatsby",
                        Description = "A classic 1925 novel depicting Jazz Age glamour and tragedy.",
                        Status = ItemStatus.Available,
                        Author = "F. Scott Fitzgerald",
                        Genre = "Fiction",
                        ISBN = "978-0743273565",
                        CreatedAt = DateTime.Now.AddDays(-60)
                    },
                    new BookItem
                    {
                        LibraryCode = "BK-1002",
                        Name = "To Kill a Mockingbird",
                        Description = "Pulitzer Prize-winning masterpiece about racial injustice in the Deep South.",
                        Status = ItemStatus.Borrowed,
                        Author = "Harper Lee",
                        Genre = "Classic",
                        ISBN = "978-0061120084",
                        CreatedAt = DateTime.Now.AddDays(-55)
                    },
                    new BookItem
                    {
                        LibraryCode = "BK-1003",
                        Name = "Clean Code",
                        Description = "A handbook of agile software craftsmanship and best programming practices.",
                        Status = ItemStatus.Available,
                        Author = "Robert C. Martin",
                        Genre = "Technology",
                        ISBN = "978-0132350884",
                        CreatedAt = DateTime.Now.AddDays(-50)
                    },
                    new BookItem
                    {
                        LibraryCode = "BK-1004",
                        Name = "Dune",
                        Description = "Epic science fiction novel set on the desert planet Arrakis.",
                        Status = ItemStatus.Damaged,
                        Author = "Frank Herbert",
                        Genre = "Sci-Fi",
                        ISBN = "978-0441172719",
                        CreatedAt = DateTime.Now.AddDays(-45)
                    },
                    new BookItem
                    {
                        LibraryCode = "BK-1005",
                        Name = "Atomic Habits",
                        Description = "An easy & proven way to build good habits and break bad ones.",
                        Status = ItemStatus.Available,
                        Author = "James Clear",
                        Genre = "Self-Help",
                        ISBN = "978-0735211292",
                        CreatedAt = DateTime.Now.AddDays(-40)
                    }
                };

                // 2. Seed Music (at least 4)
                var musicItems = new List<MusicItem>
                {
                    new MusicItem
                    {
                        LibraryCode = "MU-2001",
                        Name = "Abbey Road",
                        Description = "Iconic 11th studio album by the English rock band The Beatles.",
                        Status = ItemStatus.Available,
                        Artist = "The Beatles",
                        Album = "Abbey Road",
                        ReleaseYear = 1969,
                        Format = "Vinyl",
                        CreatedAt = DateTime.Now.AddDays(-35)
                    },
                    new MusicItem
                    {
                        LibraryCode = "MU-2002",
                        Name = "Thriller",
                        Description = "Best-selling album of all time featuring Billie Jean & Beat It.",
                        Status = ItemStatus.Borrowed,
                        Artist = "Michael Jackson",
                        Album = "Thriller",
                        ReleaseYear = 1982,
                        Format = "CD",
                        CreatedAt = DateTime.Now.AddDays(-30)
                    },
                    new MusicItem
                    {
                        LibraryCode = "MU-2003",
                        Name = "The Dark Side of the Moon",
                        Description = "Concept rock album exploring conflict, greed, and mental illness.",
                        Status = ItemStatus.Available,
                        Artist = "Pink Floyd",
                        Album = "The Dark Side of the Moon",
                        ReleaseYear = 1973,
                        Format = "Vinyl",
                        CreatedAt = DateTime.Now.AddDays(-25)
                    },
                    new MusicItem
                    {
                        LibraryCode = "MU-2004",
                        Name = "Kind of Blue",
                        Description = "Legendary studio album regarded as a jazz masterpiece.",
                        Status = ItemStatus.Destroy,
                        Artist = "Miles Davis",
                        Album = "Kind of Blue",
                        ReleaseYear = 1959,
                        Format = "CD",
                        CreatedAt = DateTime.Now.AddDays(-20)
                    }
                };

                // 3. Seed Toys (at least 4)
                var toys = new List<ToyItem>
                {
                    new ToyItem
                    {
                        LibraryCode = "TY-3001",
                        Name = "LEGO Classic Medium Creative Brick Box",
                        Description = "Building set with 484 pieces in 35 different colors.",
                        Status = ItemStatus.Available,
                        ToyType = "Building Blocks",
                        TargetAgeGroup = "4-99",
                        Manufacturer = "LEGO Group",
                        CreatedAt = DateTime.Now.AddDays(-18)
                    },
                    new ToyItem
                    {
                        LibraryCode = "TY-3002",
                        Name = "Catan Board Game",
                        Description = "Popular strategy game of resource trading and settlement building.",
                        Status = ItemStatus.Borrowed,
                        ToyType = "Board Game",
                        TargetAgeGroup = "10+",
                        Manufacturer = "KOSMOS",
                        CreatedAt = DateTime.Now.AddDays(-15)
                    },
                    new ToyItem
                    {
                        LibraryCode = "TY-3003",
                        Name = "Rubik's 3x3 Speed Cube",
                        Description = "Classic color-matching puzzle that can be enjoyed anywhere.",
                        Status = ItemStatus.Available,
                        ToyType = "Puzzle",
                        TargetAgeGroup = "8+",
                        Manufacturer = "Spin Master",
                        CreatedAt = DateTime.Now.AddDays(-12)
                    },
                    new ToyItem
                    {
                        LibraryCode = "TY-3004",
                        Name = "Monopoly Classic Edition",
                        Description = "Fast-dealing property trading board game for families.",
                        Status = ItemStatus.Available,
                        ToyType = "Board Game",
                        TargetAgeGroup = "8+",
                        Manufacturer = "Hasbro",
                        CreatedAt = DateTime.Now.AddDays(-10)
                    }
                };

                context.BookItems.AddRange(books);
                context.MusicItems.AddRange(musicItems);
                context.ToyItems.AddRange(toys);
                context.SaveChanges();

                // 4. Seed Borrowers (at least 5)
                var borrowers = new List<Borrower>
                {
                    new Borrower
                    {
                        CardNumber = "BW-101",
                        FullName = "Alice Johnson",
                        Email = "alice@example.com",
                        Phone = "555-0101",
                        Address = "123 Maple Street",
                        MembershipDate = DateTime.Now.AddDays(-90)
                    },
                    new Borrower
                    {
                        CardNumber = "BW-102",
                        FullName = "Bob Smith",
                        Email = "bob@example.com",
                        Phone = "555-0102",
                        Address = "456 Oak Avenue",
                        MembershipDate = DateTime.Now.AddDays(-80)
                    },
                    new Borrower
                    {
                        CardNumber = "BW-103",
                        FullName = "Charlie Brown",
                        Email = "charlie@example.com",
                        Phone = "555-0103",
                        Address = "789 Pine Road",
                        MembershipDate = DateTime.Now.AddDays(-70)
                    },
                    new Borrower
                    {
                        CardNumber = "BW-104",
                        FullName = "Diana Prince",
                        Email = "diana@example.com",
                        Phone = "555-0104",
                        Address = "321 Elm Boulevard",
                        MembershipDate = DateTime.Now.AddDays(-60)
                    },
                    new Borrower
                    {
                        CardNumber = "BW-105",
                        FullName = "Evan Wright",
                        Email = "evan@example.com",
                        Phone = "555-0105",
                        Address = "654 Birch Drive",
                        MembershipDate = DateTime.Now.AddDays(-50)
                    }
                };

                context.Borrowers.AddRange(borrowers);
                context.SaveChanges();

                // 5. Seed BorrowTransactions (active, returned, overdue, with fine)
                var bk1002 = context.LibraryItems.First(i => i.LibraryCode == "BK-1002");
                var mu2002 = context.LibraryItems.First(i => i.LibraryCode == "MU-2002");
                var ty3002 = context.LibraryItems.First(i => i.LibraryCode == "TY-3002");
                var bk1001 = context.LibraryItems.First(i => i.LibraryCode == "BK-1001");
                var bk1003 = context.LibraryItems.First(i => i.LibraryCode == "BK-1003");

                var alice = context.Borrowers.First(b => b.CardNumber == "BW-101");
                var bob = context.Borrowers.First(b => b.CardNumber == "BW-102");
                var charlie = context.Borrowers.First(b => b.CardNumber == "BW-103");
                var diana = context.Borrowers.First(b => b.CardNumber == "BW-104");

                var transactions = new List<BorrowTransaction>
                {
                    // Active Normal Loan
                    new BorrowTransaction
                    {
                        LibraryItemId = bk1002.Id,
                        BorrowerId = alice.Id,
                        BorrowDate = DateTime.Now.AddDays(-5),
                        DueDate = DateTime.Now.AddDays(9),
                        ReturnDate = null,
                        FineAmount = 0.00m,
                        IsFinePaid = false,
                        Notes = "Active standard loan"
                    },
                    // Active Overdue Loan (Due 4 days ago)
                    new BorrowTransaction
                    {
                        LibraryItemId = mu2002.Id,
                        BorrowerId = bob.Id,
                        BorrowDate = DateTime.Now.AddDays(-18),
                        DueDate = DateTime.Now.AddDays(-4),
                        ReturnDate = null,
                        FineAmount = 0.00m,
                        IsFinePaid = false,
                        Notes = "Active overdue loan - fine accumulating"
                    },
                    // Active Normal Loan
                    new BorrowTransaction
                    {
                        LibraryItemId = ty3002.Id,
                        BorrowerId = charlie.Id,
                        BorrowDate = DateTime.Now.AddDays(-3),
                        DueDate = DateTime.Now.AddDays(11),
                        ReturnDate = null,
                        FineAmount = 0.00m,
                        IsFinePaid = false,
                        Notes = "Active toy checkout"
                    },
                    // Historical Returned Loan (Returned on time)
                    new BorrowTransaction
                    {
                        LibraryItemId = bk1001.Id,
                        BorrowerId = diana.Id,
                        BorrowDate = DateTime.Now.AddDays(-30),
                        DueDate = DateTime.Now.AddDays(-16),
                        ReturnDate = DateTime.Now.AddDays(-18),
                        FineAmount = 0.00m,
                        IsFinePaid = true,
                        Notes = "Returned on time"
                    },
                    // Historical Returned Loan (Returned 3 days late, fine = 3 * 1.50 = 4.50, Paid)
                    new BorrowTransaction
                    {
                        LibraryItemId = bk1003.Id,
                        BorrowerId = alice.Id,
                        BorrowDate = DateTime.Now.AddDays(-25),
                        DueDate = DateTime.Now.AddDays(-11),
                        ReturnDate = DateTime.Now.AddDays(-8),
                        FineAmount = 4.50m,
                        IsFinePaid = true,
                        Notes = "Returned 3 days late. Fine paid."
                    },
                    // Historical Returned Loan (Returned 5 days late, fine = 5 * 1.50 = 7.50, Unpaid)
                    new BorrowTransaction
                    {
                        LibraryItemId = bk1001.Id,
                        BorrowerId = bob.Id,
                        BorrowDate = DateTime.Now.AddDays(-40),
                        DueDate = DateTime.Now.AddDays(-26),
                        ReturnDate = DateTime.Now.AddDays(-21),
                        FineAmount = 7.50m,
                        IsFinePaid = false,
                        Notes = "Returned 5 days late. Fine outstanding."
                    }
                };

                context.BorrowTransactions.AddRange(transactions);
                context.SaveChanges();
            }
        }
    }
}

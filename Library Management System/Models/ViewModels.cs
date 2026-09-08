using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }

    public class ItemFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Item Type is required.")]
        [Display(Name = "Item Type")]
        public string ItemType { get; set; } = "Book"; // Book, Music, Toy

        [Required(ErrorMessage = "Item Name is required.")]
        [Display(Name = "Item Name")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Library Code is required.")]
        [Display(Name = "Library Code")]
        public string LibraryCode { get; set; } = string.Empty;

        public ItemStatus Status { get; set; } = ItemStatus.Available;

        // Book fields
        public string? Author { get; set; }
        public string? Genre { get; set; }
        public string? ISBN { get; set; }

        // Music fields
        public string? Artist { get; set; }

        [Display(Name = "Release Year")]
        public int ReleaseYear { get; set; } = DateTime.Now.Year;

        public string? Album { get; set; }
        public string? Format { get; set; }

        // Toy fields
        [Display(Name = "Toy Type")]
        public string? ToyType { get; set; }

        [Display(Name = "Target Age Group")]
        public string? TargetAgeGroup { get; set; }

        public string? Manufacturer { get; set; }
    }

    public class BorrowItemViewModel
    {
        [Display(Name = "Select Item to Borrow")]
        public int LibraryItemId { get; set; }

        [Display(Name = "Or Enter Library Code")]
        public string? DirectLibraryCode { get; set; }

        [Display(Name = "Select Borrower")]
        public int BorrowerId { get; set; }

        [Display(Name = "Or Enter Card Number")]
        public string? DirectCardNumber { get; set; }

        [Display(Name = "Loan Duration (Days)")]
        public int DaysToBorrow { get; set; } = 14;

        public string? Notes { get; set; }
    }

    public class ReturnItemViewModel
    {
        public int TransactionId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string LibraryCode { get; set; } = string.Empty;
        public string BorrowerName { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime ReturnDate { get; set; } = DateTime.Now;
        public int DaysLate { get; set; }
        public decimal FineAmount { get; set; }
        public bool IsFinePaid { get; set; }
        public string? Notes { get; set; }
    }

    public class ManagerDashboardViewModel
    {
        // Borrowing Statistics
        public int TotalTransactions { get; set; }
        public int ActiveLoans { get; set; }
        public int ReturnedLoans { get; set; }
        public int OverdueLoans { get; set; }

        // Item Status Statistics
        public int AvailableCount { get; set; }
        public int BorrowedCount { get; set; }
        public int DamagedCount { get; set; }
        public int DestroyCount { get; set; }
        public int TotalItems { get; set; }
        public int BooksCount { get; set; }
        public int MusicCount { get; set; }
        public int ToysCount { get; set; }

        // Fine Statistics
        public decimal TotalFines { get; set; }
        public decimal TotalPaidFines { get; set; }
        public decimal TotalUnpaidFines { get; set; }
        public int FinedTransactionsCount { get; set; }
    }

    public class PublicSearchViewModel
    {
        public string? SearchTerm { get; set; }
        public string SelectedItemType { get; set; } = "All"; // All, Book, Music, Toy
        public string SelectedAvailability { get; set; } = "All"; // All, Available, Borrowed
        public List<LibraryItem> Results { get; set; } = new List<LibraryItem>();
    }
}

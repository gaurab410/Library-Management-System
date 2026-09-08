using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.Models
{
    public class BorrowTransaction
    {
        public int Id { get; set; }

        [Required]
        public int LibraryItemId { get; set; }

        [ForeignKey("LibraryItemId")]
        public virtual LibraryItem LibraryItem { get; set; } = default!;

        [Required]
        public int BorrowerId { get; set; }

        [ForeignKey("BorrowerId")]
        public virtual Borrower Borrower { get; set; } = default!;

        [Display(Name = "Borrow Date")]
        public DateTime BorrowDate { get; set; } = DateTime.Now;

        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; }

        [Display(Name = "Return Date")]
        public DateTime? ReturnDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Fine Amount ($)")]
        public decimal FineAmount { get; set; } = 0.00m;

        [Display(Name = "Fine Paid")]
        public bool IsFinePaid { get; set; } = false;

        public string? Notes { get; set; }

        [NotMapped]
        public bool IsOverdue => ReturnDate == null ? DateTime.Now > DueDate : ReturnDate > DueDate;

        [NotMapped]
        public int DaysLate
        {
            get
            {
                var endDate = ReturnDate ?? DateTime.Now;
                if (endDate > DueDate)
                {
                    return (endDate.Date - DueDate.Date).Days;
                }
                return 0;
            }
        }
    }
}

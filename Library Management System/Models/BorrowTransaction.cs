using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class Borrower
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Card Number is required.")]
        [Display(Name = "Card Number")]
        public string CardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full Name is required.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? Address { get; set; }

        [Display(Name = "Active Member")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Membership Date")]
        public DateTime MembershipDate { get; set; } = DateTime.Now;

        // Navigation property for borrowing history
        public virtual ICollection<BorrowTransaction> BorrowTransactions { get; set; } = new List<BorrowTransaction>();
    }
}

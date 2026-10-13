using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.Models
{
    public abstract class LibraryItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Item name is required.")]
        [Display(Name = "Item Name")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Library Code is required.")]
        [Display(Name = "Library Code")]
        public string LibraryCode { get; set; } = string.Empty;

        [Display(Name = "Status")]
        public ItemStatus Status { get; set; } = ItemStatus.Available;

        [Required(ErrorMessage = "Branch is required.")]
        [Display(Name = "Branch")]
        public int BranchId { get; set; }

        [ForeignKey("BranchId")]
        [Display(Name = "Branch")]
        [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
        public virtual Branch Branch { get; set; } = default!;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property for borrowing history
        public virtual ICollection<BorrowTransaction> BorrowTransactions { get; set; } = new List<BorrowTransaction>();

        [NotMapped]
        public string ItemType => GetType().Name.Replace("Item", "");
    }
}

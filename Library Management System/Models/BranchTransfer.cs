using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.Models
{
    public enum TransferStatus
    {
        [Display(Name = "Requested")]
        Requested,

        [Display(Name = "In Transit")]
        InTransit,

        [Display(Name = "Received")]
        Received,

        [Display(Name = "Cancelled")]
        Cancelled
    }

    public class BranchTransfer
    {
        [Key]
        [Display(Name = "Transfer ID")]
        public int BranchTransferId { get; set; }

        [Required(ErrorMessage = "Library Item is required.")]
        [Display(Name = "Library Item")]
        public int LibraryItemId { get; set; }

        [ForeignKey("LibraryItemId")]
        [Display(Name = "Library Item")]
        [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
        public virtual LibraryItem LibraryItem { get; set; } = default!;

        [Required(ErrorMessage = "Origin Branch is required.")]
        [Display(Name = "Origin Branch")]
        public int FromBranchId { get; set; }

        [ForeignKey("FromBranchId")]
        [Display(Name = "Origin Branch")]
        [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
        public virtual Branch FromBranch { get; set; } = default!;

        [Required(ErrorMessage = "Destination Branch is required.")]
        [Display(Name = "Destination Branch")]
        public int ToBranchId { get; set; }

        [ForeignKey("ToBranchId")]
        [Display(Name = "Destination Branch")]
        [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
        public virtual Branch ToBranch { get; set; } = default!;

        [Display(Name = "Status")]
        public TransferStatus Status { get; set; } = TransferStatus.Requested;

        [Display(Name = "Requested At")]
        public DateTime RequestedAt { get; set; } = DateTime.Now;

        [Display(Name = "Requested By Role")]
        public string RequestedByRole { get; set; } = string.Empty;

        [Display(Name = "Completed At")]
        public DateTime? CompletedAt { get; set; }

        [Display(Name = "Notes / Reason")]
        [StringLength(500)]
        public string? Notes { get; set; }
    }
}

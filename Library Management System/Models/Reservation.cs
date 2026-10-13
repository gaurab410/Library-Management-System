using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.Models
{
    public enum ReservationStatus
    {
        [Display(Name = "Pending")]
        Pending,

        [Display(Name = "Ready for Pickup")]
        ReadyForPickup,

        [Display(Name = "Fulfilled")]
        Fulfilled,

        [Display(Name = "Expired")]
        Expired,

        [Display(Name = "Cancelled")]
        Cancelled
    }

    public class Reservation
    {
        [Key]
        public int ReservationId { get; set; }

        [Required]
        [Display(Name = "Library Item")]
        public int LibraryItemId { get; set; }

        [ForeignKey("LibraryItemId")]
        public virtual LibraryItem LibraryItem { get; set; } = default!;

        [Required]
        [Display(Name = "Borrower")]
        public int BorrowerId { get; set; }

        [ForeignKey("BorrowerId")]
        public virtual Borrower Borrower { get; set; } = default!;

        [Display(Name = "Queue Position")]
        public int QueuePosition { get; set; }

        [Display(Name = "Status")]
        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

        [Display(Name = "Reserved At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Notified At")]
        public DateTime? NotifiedAt { get; set; }

        [Display(Name = "Expires At")]
        public DateTime? ExpiresAt { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }
    }
}

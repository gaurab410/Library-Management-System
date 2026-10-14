using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagementSystem.Models
{
    public enum NotificationType
    {
        [Display(Name = "Due Soon")]
        DueSoon,

        [Display(Name = "Overdue")]
        Overdue,

        [Display(Name = "Fine Issued")]
        FineIssued,

        [Display(Name = "Reservation Ready")]
        ReservationReady,

        [Display(Name = "Transfer Update")]
        TransferUpdate
    }

    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        [Display(Name = "Borrower")]
        public int BorrowerId { get; set; }

        [ForeignKey("BorrowerId")]
        public virtual Borrower Borrower { get; set; } = default!;

        [Display(Name = "Library Item")]
        public int? LibraryItemId { get; set; }

        [ForeignKey("LibraryItemId")]
        public virtual LibraryItem? LibraryItem { get; set; }

        [Required(ErrorMessage = "Notification message is required.")]
        [Display(Name = "Message")]
        public string Message { get; set; } = string.Empty;

        [Display(Name = "Notification Type")]
        public NotificationType Type { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Read Status")]
        public bool IsRead { get; set; } = false;
    }
}

using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class Branch
    {
        [Key]
        public int BranchId { get; set; }

        [Required(ErrorMessage = "Branch Name is required.")]
        [Display(Name = "Branch Name")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Branch Address is required.")]
        [Display(Name = "Address")]
        [StringLength(250)]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        [Phone]
        [StringLength(30)]
        public string? Phone { get; set; }

        [Display(Name = "Email Address")]
        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [Display(Name = "Opening Hours")]
        [StringLength(100)]
        public string? OpeningHours { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation property for inventory items located at this branch
        public virtual ICollection<LibraryItem> Items { get; set; } = new List<LibraryItem>();
    }
}

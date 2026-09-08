using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class ToyItem : LibraryItem
    {
        [Display(Name = "Toy Type")]
        public string? ToyType { get; set; }

        [Display(Name = "Target Age Group")]
        public string? TargetAgeGroup { get; set; }

        public string? Manufacturer { get; set; }
    }
}

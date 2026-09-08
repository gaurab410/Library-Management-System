using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class MusicItem : LibraryItem
    {
        [Required(ErrorMessage = "Artist is required.")]
        public string Artist { get; set; } = string.Empty;

        [Display(Name = "Release Year")]
        [Range(1800, 2100, ErrorMessage = "Please enter a valid year.")]
        public int ReleaseYear { get; set; }

        public string? Album { get; set; }

        public string? Format { get; set; }
    }
}

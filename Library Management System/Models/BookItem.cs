using System.ComponentModel.DataAnnotations;

namespace LibraryManagementSystem.Models
{
    public class BookItem : LibraryItem
    {
        [Required(ErrorMessage = "Author is required.")]
        public string Author { get; set; } = string.Empty;

        [Required(ErrorMessage = "Genre is required.")]
        public string Genre { get; set; } = string.Empty;

        public string? ISBN { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Models
{
    public class Author
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Author name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be 2-100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Bio cannot exceed 1000 characters")]
        public string? Bio { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // One to Many — Ek author ki kai books
        public List<Book> Books { get; set; } = new List<Book>();
    }
}
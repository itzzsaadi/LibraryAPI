using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be 1-200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "ISBN is required")]
        [StringLength(13, MinimumLength = 10, ErrorMessage = "ISBN must be 10-13 characters")]
        public string ISBN { get; set; } = string.Empty;

        [Range(0, 1000, ErrorMessage = "Total copies must be between 0 and 1000")]
        public int TotalCopies { get; set; }

        [Range(0, 1000, ErrorMessage = "Available copies must be between 0 and 1000")]
        public int AvailableCopies { get; set; }

        public string? CoverImagePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Key
        public int AuthorId { get; set; }
        public Author? Author { get; set; }
    }
}
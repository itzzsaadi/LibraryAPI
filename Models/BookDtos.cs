using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace LibraryAPI.Models
{
    public class BookCreateDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be 1-200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "ISBN is required")]
        [StringLength(13, MinimumLength = 10, ErrorMessage = "ISBN must be 10-13 characters")]
        public string ISBN { get; set; } = string.Empty;

        [Required(ErrorMessage = "Author is required")]
        public int AuthorId { get; set; }

        [Range(1, 1000, ErrorMessage = "Total copies must be between 1 and 1000")]
        public int TotalCopies { get; set; }

        // File upload — optional
        public IFormFile? CoverImage { get; set; }
    }

    public class BookUpdateDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be 1-200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "ISBN is required")]
        [StringLength(13, MinimumLength = 10, ErrorMessage = "ISBN must be 10-13 characters")]
        public string ISBN { get; set; } = string.Empty;

        [Range(1, 1000, ErrorMessage = "Total copies must be between 1 and 1000")]
        public int TotalCopies { get; set; }

        public IFormFile? CoverImage { get; set; }
    }

    public class BookResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public string? CoverImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }

        // Author info
        public int AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
    }
}
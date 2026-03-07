using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Models
{
    public class AuthorCreateDto
    {
        [Required(ErrorMessage = "Author name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be 2-100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Bio cannot exceed 1000 characters")]
        public string? Bio { get; set; }
    }

    public class AuthorUpdateDto
    {
        [Required(ErrorMessage = "Author name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be 2-100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Bio cannot exceed 1000 characters")]
        public string? Bio { get; set; }
    }

    public class AuthorResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalBooks { get; set; }
    }
}
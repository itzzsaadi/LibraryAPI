using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string ISBN { get; set; }

        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }

        public string? CoverImagePath { get; set; }

        // Foreign Key
        public int AuthorId { get; set; }
        public Author? Author { get; set; }
    }
}
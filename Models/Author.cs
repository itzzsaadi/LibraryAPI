using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Models
{
    public class Author
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Bio { get; set; }

        // One to Many — Ek author ki kai books
        public List<Book> Books { get; set; } = new List<Book>();
    }
}
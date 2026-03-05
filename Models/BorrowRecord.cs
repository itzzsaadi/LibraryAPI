using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Models
{
    public class BorrowRecord
    {
        public int Id { get; set; }

        public DateTime BorrowDate { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }

        public bool IsReturned { get; set; } = false;

        // Foreign Keys
        public string MemberId { get; set; } = string.Empty; 
        public Member? Member { get; set; }

        public int BookId { get; set; } 
        public Book? Book { get; set; }
    }
}
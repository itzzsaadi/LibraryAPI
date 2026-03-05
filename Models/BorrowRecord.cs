namespace LibraryAPI.Models
{
    public class BorrowRecord
    {
        public int Id { get; set; }

        public DateTime BorrowDate { get; set; } = DateTime.Now;
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }

        public bool IsReturned { get; set; } = false;

        // Foreign Keys
        public int MemberId { get; set; }
        public Member? Member { get; set; }

        public int BookId { get; set; }
        public Book? Book { get; set; }
    }
}
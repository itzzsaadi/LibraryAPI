using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace LibraryAPI.Models
{
    public class Member
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string Phone { get; set; }

        public DateTime JoinDate { get; set; } = DateTime.Now;

        // One to Many
        public List<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
    }
}
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Models
{
    public class Member : IdentityUser
    {
        // Id, Email, UserName, PasswordHash — ye sab IdentityUser se already aa rahe hain
        [Required]
        public string FullName { get; set; } = string.Empty;

        public string? Phone { get; set; }  // ? = optional field

        public DateTime JoinDate { get; set; } = DateTime.UtcNow;  // UtcNow better hai

        // Navigation Property
        public List<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
    }
}
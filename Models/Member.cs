using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Models
{
    public enum MembershipType { Basic, Premium }
    public enum MemberStatus { Active, Suspended, Expired }
    public class Member : IdentityUser
    {
        // Id, Email, UserName, PasswordHash ye sab IdentityUser se already aa rahe hain
        [Required]
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }  // ? = optional field
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;  // UtcNow better hai
        
        // Membership
        public MembershipType MembershipType { get; set; } = MembershipType.Basic;
        public MemberStatus Status { get; set; } = MemberStatus.Active;
        public DateTime MembershipExpiryDate { get; set; } = DateTime.UtcNow.AddYears(1);
        public int MaxBooksAllowed { get; set; } = 3;
        public int CurrentBooksCount { get; set; } = 0;

        // Email Verification
        public string? EmailOtp { get; set; }
        public DateTime? OtpExpiry { get; set; }
        
        // Password Reset
        public string? PasswordResetOtp { get; set; }
        public DateTime? PasswordResetOtpExpiry { get; set; }
        
        // Refresh Token
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
        
        // Navigation Property
        public List<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
    }
}
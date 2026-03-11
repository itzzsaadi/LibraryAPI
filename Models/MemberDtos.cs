using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Models
{
    // Admin ke liye — Member banao
    public class MemberCreateDto
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be 3-100 characters")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "Password must have uppercase, lowercase, number and special character")]
        public string Password { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        public string? Phone { get; set; }

        public MembershipType MembershipType { get; set; } = MembershipType.Basic;
    }

    // Admin ke liye — Member update karo
    public class MemberUpdateDto
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be 3-100 characters")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        public string? Phone { get; set; }

        public MembershipType MembershipType { get; set; }
        public MemberStatus Status { get; set; }
        public DateTime MembershipExpiryDate { get; set; }
    }

    // Member khud apni profile update kare
    public class MemberProfileUpdateDto
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be 3-100 characters")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        public string? Phone { get; set; }
    }

    // Response DTO
    public class MemberResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime JoinDate { get; set; }
        public MembershipType MembershipType { get; set; }
        public MemberStatus Status { get; set; }
        public DateTime MembershipExpiryDate { get; set; }
        public int MaxBooksAllowed { get; set; }
        public int CurrentBooksCount { get; set; }
    }
    public class UpdateStatusDto
    {
        [Required]
        public MemberStatus Status { get; set; }
    }
}
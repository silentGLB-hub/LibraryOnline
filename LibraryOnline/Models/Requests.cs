using System.ComponentModel.DataAnnotations;

namespace LibraryOnline.Models
{
    public class LoginInput
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class RegisterInput : LoginInput
    {
        [Required, StringLength(100)]
        public string FullName { get; set; }
    }

    public class ProfileInput
    {
        [Required, StringLength(100)]
        public string FullName { get; set; }

        [Phone, StringLength(30)]
        public string PhoneNumber { get; set; }
        public int[] Genres { get; set; }
    }

    public class PasswordInput
    {
        [Required]
        public string OldPassword { get; set; }

        [Required]
        public string NewPassword { get; set; }
    }

    public class ReserveInput
    {
        public int BookId { get; set; }
    }

    public class ActionInput
    {
        [Required]
        public string Action { get; set; }
    }

    public class DecisionInput
    {
        public bool Approve { get; set; }

        [StringLength(400)]
        public string Note { get; set; }
    }

    public class PaymentInput
    {
        public decimal Amount { get; set; }

        [StringLength(200)]
        public string Note { get; set; }
    }

    public class NameInput
    {
        [Required, StringLength(80)]
        public string Name { get; set; }
    }

    public class UserInput
    {
        [Required]
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateUserInput : RegisterInput
    {
        [Required]
        public string Role { get; set; }
    }

    public class BookInput
    {
        [Required, StringLength(200)]
        public string Title { get; set; }

        [Required, StringLength(150)]
        public string Author { get; set; }

        [Required, StringLength(150)]
        public string Publisher { get; set; }

        [Range(1000, 2100)]
        public int PublicationYear { get; set; }

        [
            Required,
            RegularExpression(
                @"(?:[0-9]{13}|[0-9]{9}[0-9Xx])",
                ErrorMessage = "ISBN cần 10 hoặc 13 ký tự, không có dấu gạch."
            )
        ]
        public string ISBN { get; set; }

        [Required, StringLength(5000)]
        public string Description { get; set; }

        [StringLength(1000)]
        public string CoverImage { get; set; }
        public int CategoryId { get; set; }

        [Range(1, 10000)]
        public int TotalCopies { get; set; }
        public bool IsActive { get; set; } = true;
        public string Version { get; set; }
    }

    public class PolicyInput
    {
        [Range(1, 30)]
        public int MaxActiveLoans { get; set; }

        [Range(1, 180)]
        public int LoanDays { get; set; }

        [Range(1, 14)]
        public int PickupDays { get; set; }

        [Range(1, 90)]
        public int RenewalDays { get; set; }

        [Range(0, 10)]
        public int MaxRenewals { get; set; }

        [Range(0, 1000000)]
        public decimal FinePerDay { get; set; }

        [Required, StringLength(100)]
        public string LibraryName { get; set; }
    }
}

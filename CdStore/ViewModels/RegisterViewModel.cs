using System.ComponentModel.DataAnnotations;

namespace CdStore.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Nazwa jest wymagana")]
        [MaxLength(200)]

        public string Name { get; set; }

        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress]
        [MaxLength(200)]

        public string Email { get; set; }

        [Required(ErrorMessage = "Hasło jest wymagane")]
        [StringLength(35, MinimumLength = 8, ErrorMessage = ("The {0} must be at {2} and at max {1} characters long"))]
        [DataType(DataType.Password)]
        [Compare("ConfirmPassword", ErrorMessage = "Hasła się nie zgadzają")]

        public string Password { get; set; }

        [Required(ErrorMessage = "Powtórzenie hasła jest wymagane")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]

        public string ConfirmPassword { get; set; }
    }
}

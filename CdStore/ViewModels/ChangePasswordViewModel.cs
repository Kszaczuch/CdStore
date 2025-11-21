using System.ComponentModel.DataAnnotations;

namespace CdStore.ViewModels
{
    public class ChangePasswordViewModel
    {

        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress]

        public string Email { get; set; }

        [Required(ErrorMessage = "Nowe hasło jest wymagane")]
        [StringLength(35, MinimumLength = 8, ErrorMessage = ("The {0} must be at {2} and at max {1} characters long"))]
        [DataType(DataType.Password)]
        [Compare("ConfirmNewPassword", ErrorMessage = "Hasła się nie zgadzają")]

        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Powtórzenie nowego hasła jest wymagane")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]

        public string ConfirmNewPassword { get; set; }
    }
}

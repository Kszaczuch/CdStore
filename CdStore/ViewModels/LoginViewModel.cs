using System.ComponentModel.DataAnnotations;

namespace CdStore.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress]  

        public string Email { get; set; }

        [Required(ErrorMessage = "Hasło jest wymagane")]
        [DataType(DataType.Password)]
        
        public string Password { get; set; }

        [Display(Name = "Zapamiętaj mnie?")]

        public bool RememberMe { get; set; }

    }
}

using System.ComponentModel.DataAnnotations;

namespace CdStore.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress]  

        public string email { get; set; }

        [Required(ErrorMessage = "Hasło jest wymagane")]
        [DataType(DataType.Password)]
        
        public string password { get; set; }

        [Display(Name = "Zapamiętaj mnie?")]

        public bool RememberMe { get; set; }

    }
}

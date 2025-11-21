using System.ComponentModel.DataAnnotations;

namespace CdStore.ViewModels
{
    public class VerifyEmailViewModel
    {
        [Required(ErrorMessage = "Email jest wymagany")]
        [EmailAddress]
        public string Email { get; set; }
    }
}

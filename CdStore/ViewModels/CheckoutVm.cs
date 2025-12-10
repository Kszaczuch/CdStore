using CdStore.Models;
using System.ComponentModel.DataAnnotations;

namespace CdStore.ViewModels
{
    public class CheckoutVm
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        [Required]
        [MaxLength(300)]
        public string Address { get; set; }

        [Required]
        [Phone]
        [MaxLength(30)]
        public string Phone { get; set; }

        [Required, EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; }

        public List<Album> CartItems { get; set; } = new List<Album>();
        public Dictionary<int, int> Quantities { get; set; } = new Dictionary<int, int>();

        public decimal Total { get; set; }
    }
}
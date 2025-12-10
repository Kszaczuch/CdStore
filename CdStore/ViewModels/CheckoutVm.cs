using CdStore.Models;
using System.ComponentModel.DataAnnotations;

namespace CdStore.ViewModels
{
    public class CheckoutVm
    {
        [Required] public string FirstName { get; set; }
        [Required] public string LastName { get; set; }
        [Required] public string Address { get; set; }
        [Required] public string Phone { get; set; }
        [Required, EmailAddress] public string Email { get; set; }

        public List<Album> CartItems { get; set; } = new List<Album>();
        public Dictionary<int, int> Quantities { get; set; } = new Dictionary<int, int>();

        public decimal Total { get; set; }
    }
}
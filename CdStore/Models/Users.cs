using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CdStore.Models
{
    public class Users : IdentityUser
    {
        [MaxLength(200)]
        public string FullName { get; set; }

        public bool IsBlocked { get; set; } = false;

        [MaxLength(300)]
        public string DeliveryAddress { get; set; }
    }
}
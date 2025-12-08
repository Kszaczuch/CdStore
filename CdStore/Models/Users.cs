using Microsoft.AspNetCore.Identity;

namespace CdStore.Models
{
    public class Users : IdentityUser
    {
        public string FullName { get; set; }
        public string DeliveryAddress { get; set; }
    }
}
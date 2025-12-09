using Microsoft.AspNetCore.Identity;

namespace CdStore.Models
{
    public class Users : IdentityUser
    {
        public string FullName { get; set; }

        public bool IsBlocked { get; set; } = false;
    }
}

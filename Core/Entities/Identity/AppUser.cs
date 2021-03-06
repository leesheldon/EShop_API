using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Core.Entities.Identity
{
    public class AppUser : IdentityUser
    {
        public string DisplayName { get; set; }
        public Address Address { get; set; }

        [Display(Name = "Lockout Reason")]
        public string LockoutReason { get; set; }

        [Display(Name = "UnLock Reason")]
        public string UnLockReason { get; set; }
        
        [NotMapped]
        public bool IsLockedOut { get; set; }

        [NotMapped]
        public string RolesNames { get; set; }
           
    }
}

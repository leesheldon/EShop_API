using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Dtos
{
    public class UsersWithRolesToReturnDto : UserToUpdateDto
    {
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
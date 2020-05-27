using System;

namespace API.Dtos
{
    public class UserToLockOrUnlockDto
    {
        public string Id { get; set; }

        public DateTimeOffset? LockoutEnd { get; set; }
        
        public int AccessFailedCount { get; set; }

        public string LockoutReason { get; set; }
        
        public string UnLockReason { get; set; }

    }
}
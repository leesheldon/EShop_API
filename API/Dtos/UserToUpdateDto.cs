using System;
using System.Collections.Generic;
using Core.Entities.Identity;

namespace API.Dtos
{
    public class UserToUpdateDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string PhoneNumber { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }

        public List<RolesListOfSelectedUser> RolesList { get; set; }
        
    }
}
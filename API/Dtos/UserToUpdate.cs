using System.Collections.Generic;
using Core.Entities.Identity;

namespace API.Dtos
{
    public class UserToUpdate
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string PhoneNumber { get; set; }
        
        public List<RolesListOfSelectedUser> RolesList { get; set; }
        
    }
}
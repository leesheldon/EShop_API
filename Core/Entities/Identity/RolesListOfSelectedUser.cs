using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities.Identity
{
    [NotMapped]
    public class RolesListOfSelectedUser
    {
        public string Id { get; set; }

        public string Name { get; set; }
        
        public bool SelectedRole { get; set; }

    }
}
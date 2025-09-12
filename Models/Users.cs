using Microsoft.AspNetCore.Identity;

namespace Ward_Management_System.Models
{
    public class Users : IdentityUser
    {
        public string FullName { get; set; }
        public int Age { get; set; }
        public string Address { get; set; }
        public string IdNumber { get; set; }
        public string Gender { get; set; }
        public bool IsActive { get; set; } = true;
        public string? ProfileImagePath { get; set; }
        public DateTime DateAdded { get; set; } = DateTime.Now;
        public ICollection<EmergencyContact> EmergencyContacts { get; set; }
    }
}
 
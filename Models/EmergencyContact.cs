using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Ward_Management_System.Models
{
    public class EmergencyContact
    {
        [Key]
        public int EmergencyID { get; set; }

        [Required]
        public string EmergencyName { get; set; }

        [Required]
        public string EmergencyPhone { get; set; }

        [Required]
        public string EmergencyEmail { get; set; }

        [Required]
        public string EmergencyRelationship { get; set; }

        //Navigation Property
        [ValidateNever]
        public Users User { get; set; }

        //Foreign Key
        public string UserId { get; set; }

    }
}

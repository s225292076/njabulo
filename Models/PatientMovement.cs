using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ward_Management_System.Models
{
    public class PatientMovement
    {
        [Key]
        public int MovementID { get; set; }

        [ForeignKey("Admission")]
        public int AdmissionId { get; set; }
        public Admissions Admission { get; set; }

        [ForeignKey("Ward")]
        public int WardId { get; set; }
        public Wards Ward { get; set; }

        // Default to current time if not set
        public DateTime MovementTime { get; set; } = DateTime.Now;

        [MaxLength(255)]
        public string Notes { get; set; }
    }

}

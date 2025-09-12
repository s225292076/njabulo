using Ward_Management_System.DTOs;
using Ward_Management_System.Models;

namespace Ward_Management_System.ViewModels
{
    public class PatientMovementViewModel
    {
        public int AdmissionId { get; set; }
        public string PatientName { get; set; }
        public string CurrentLocation { get; set; }
        public List<MovementRecord> MovementHistory { get; set; }
        public List<Wards> AvailableWards { get; set; }
    }
}

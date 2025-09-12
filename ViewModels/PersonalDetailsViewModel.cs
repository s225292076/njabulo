namespace Ward_Management_System.ViewModels
{
    public class PersonalDetailsViewModel
    {
        // User details
        public string FullName { get; set; }
        public int Age { get; set; }
        public string IdNumber { get; set; }
        public string Address { get; set; }
        public string Gender { get; set; }
        public DateTime DateAdded { get; set; }
        public string? ProfileImagePath { get; set; }


        // Emergency Contact 1
        public string EmergencyName1 { get; set; }
        public string EmergencyPhone1 { get; set; }
        public string EmergencyEmail1 { get; set; }
        public string EmergencyRelationship1 { get; set; }

        // Emergency Contact 2 (optional)
        public string? EmergencyName2 { get; set; }
        public string? EmergencyPhone2 { get; set; }
        public string? EmergencyEmail2 { get; set; }
        public string? EmergencyRelationship2 { get; set; }
    }
}

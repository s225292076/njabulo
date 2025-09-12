using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ward_Management_System.Data;
using Ward_Management_System.DTOs;
using Ward_Management_System.Models;
using Ward_Management_System.ViewModels;

namespace Ward_Management_System.Controllers
{
    public class MovementController : Controller
    {
        public MovementController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        //POST: MovePatient
        [HttpPost]
        public IActionResult MovePatient(int admissionId, int wardId, string notes)
        {
            var movement = new PatientMovement
            {
                AdmissionId = admissionId,
                WardId = wardId,
                MovementTime = DateTime.Now,
                Notes = notes
            };

            _context.PatientMovements.Add(movement);

            var admission = _context.Admissions.FirstOrDefault(a => a.AdmissionId == admissionId);
            if (admission != null)
            {
                admission.WardId = wardId;
            }

            //var appointment = _context.Appointments.FirstOrDefault(appt => appt.AppointmentId == admission.AppointmentId);
            //if (appointment != null)
            //{
            //    appointment.WardId = wardId;
            //}
            _context.SaveChanges();

            return RedirectToAction("Movement", new { admissionId = admissionId });
        }
        //GET: Movement
        public IActionResult Movement(int admissionId)
        {
            var patient = _context.Admissions
                .Include(a => a.Appointment)
                .Include(a => a.Wards)
                .FirstOrDefault(a => a.AdmissionId == admissionId);

            var history = _context.PatientMovements
                .Where(m => m.AdmissionId == admissionId)
                .Include(m => m.Ward)
                .OrderByDescending(m => m.MovementTime)
                .Select(m => new MovementRecord
                {
                    LocationName = m.Ward.WardName,
                    MovementTime = m.MovementTime,
                    Notes = m.Notes
                })
                .ToList();

            if (patient == null)
                return NotFound("Admission not found");

            var model = new PatientMovementViewModel
            {
                AdmissionId = admissionId,
                PatientName = patient.PatientName,
                CurrentLocation = patient.Wards?.WardName ?? "Not Assigned",
                MovementHistory = history,
                AvailableWards = _context.wards.ToList()
            };

            return View("PatientMovement", model);
        }



    }
}

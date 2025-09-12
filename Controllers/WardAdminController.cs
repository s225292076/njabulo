﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Ward_Management_System.Data;
using Ward_Management_System.Models;
using Ward_Management_System.ViewModels;

namespace Ward_Management_System.Controllers
{
    public class WardAdminController : Controller
    {
        public WardAdminController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        //Ward Admin Dashboard
        [Authorize(Roles = "WardAdmin")]
        public IActionResult Index()
        {
            return View();
        }

        //Check In Page
        [Authorize(Roles = "WardAdmin")]
        public async Task<IActionResult> CheckIn(int pg = 1)
        {
            var today = DateTime.Today;

            var appointments = await _context.Appointments
                .Include(a => a.User)
                .Include(a => a.ConsultationRoom)
                .Where(a => a.PreferredDate.Date == today && a.Status != "Cancelled" && a.Status != "Admitted")
                .OrderBy(a => a.PreferredDate)
                .ThenBy(a => a.PreferredTime)
                .ToListAsync();

            ViewBag.ConsultationRooms = _context.ConsultationRooms
                                .Select(cr => new SelectListItem
                                {
                                    Value = cr.RoomId.ToString(),
                                    Text = cr.RoomName
                                }).ToList();

            ViewBag.Wards = _context.wards
                                .Select(w => new SelectListItem
                                {
                                    Value = w.WardId.ToString(),
                                    Text = w.WardName
                                }).ToList();

            const int pageSize = 5;
            if (pg < 1)
            {
                pg = 1;
            }

            int recsCount = appointments.Count();
            var pager = new Pager(recsCount, pg, pageSize);
            int recSkip = (pg - 1) * pageSize;
            var data = appointments.Skip(recSkip).Take(pageSize).ToList();
            ViewBag.Pager = pager; // Pass the pager to the view for pagination

            return View(data);
        }

       
        [HttpPost]
        public async Task<IActionResult> CheckInPatient(int id, int ConsultationRoomId)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            //check if patient is already checked in 
            var duplicateCheck = await _context.Appointments
                .AnyAsync(a => a.IdNumber == appointment.IdNumber
                            && a.FullName == appointment.FullName
                            && (a.Status == "CheckedIn" || a.Status == "Admitted"));

            if (duplicateCheck)
            {
                TempData["ToastMessage"] = "Patient is already checked in.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("CheckIn");
            }

            var room = await _context.ConsultationRooms.FindAsync(ConsultationRoomId);
            if (room == null || !room.IsAvailable)
            {
                TempData["ToastMessage"] = "Room not available.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("CheckIn");
            }

            // Assign room
            appointment.ConsultationRoomId = ConsultationRoomId;
            appointment.Status = "CheckedIn";

            // Mark room as occupied
            room.IsAvailable = false;

            _context.Update(appointment);
            _context.Update(room);
            await _context.SaveChangesAsync();

            TempData["ToastMessage"] = "Patient successfully checked in.";
            TempData["ToastType"] = "success";

            return RedirectToAction("CheckIn");
        }


        //Admit Patient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdmitPatient(Admissions admission, int AppointmentId)
        {
            //get appointment in order to check if the patient is already admitted
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.AppointmentId == AppointmentId);

            if (appointment == null)
            {
                TempData["ToastMessage"] = "Admission failed: Appointment not found.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("CheckIn");
            }

            var existingAdmission = await _context.Admissions
                .Include(ad => ad.Appointment)
                .FirstOrDefaultAsync(ad =>
                    ad.Appointment.FullName == appointment.FullName &&
                    ad.Appointment.IdNumber == appointment.IdNumber &&
                    ad.Status == appointment.Status);

            if (existingAdmission != null)
            {
                TempData["ToastMessage"] = "Admission failed: Patient is already admitted.";
                TempData["ToastType"] = "warning";
                return RedirectToAction("CheckIn");
            }

            if (ModelState.IsValid)
            {
                TempData["ToastMessage"] = "Admission failed: Invalid form submission.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("CheckIn");
            }
            appointment.Status = "Admitted";

            admission.Status = appointment.Status;
            admission.AppointmentId = appointment.AppointmentId;
            _context.Admissions.Add(admission);

            var ward = await _context.wards.FirstOrDefaultAsync(w => w.WardId == admission.WardId);
            if (ward != null)
            {
                if (ward.CurrentBedCount > 0 && ward.OccupiedBeds < ward.TotalBedCapacty)
                {
                    ward.OccupiedBeds += 1;
                    ward.CurrentBedCount -= 1;
                }
                else
                {
                    TempData["ToastMessage"] = "Admission failed: No available beds in selected ward.";
                    TempData["ToastType"] = "warning";
                    return RedirectToAction("CheckIn");
                }
            }

            await _context.SaveChangesAsync();

            TempData["ToastMessage"] = "Patient successfully admitted.";
            TempData["ToastType"] = "success";
            return RedirectToAction("CheckIn");
        }

        [Authorize(Roles = "WardAdmin")]
        public async Task<IActionResult> ViewAdmissions()
        {
            var admittedPatients = await (from ad in _context.Admissions
                                          where ad.Status != "Discharged"
                                          join ap in _context.Appointments
                                          on ad.AppointmentId equals ap.AppointmentId
                                          join w in _context.wards
                                          on ad.WardId equals w.WardId
                                          select new AdmissionViewModel
                                          {
                                              AdmissionId = ad.AdmissionId,
                                              AppointmentId = ap.AppointmentId,
                                              PatientName = ap.FullName,
                                              IdNumber = ap.IdNumber,
                                              AdmissionDate = ad.AdmissionDate,
                                              FolderStatus = _context.PatientFolder
                                                                      .Include(pf => pf.Appointment)
                                                                      .Any(pf => pf.Appointment.FullName == ap.FullName && pf.Appointment.IdNumber == ap.IdNumber)
                                                                      ? "Has Folder" : "No Folder",
                                              Condition = ad.Condition,
                                              WardName = w.WardName,
                                              Status = ad.Status

                                          }).ToListAsync();
            var checkedInPatients = await (from a in _context.Appointments
                                           where a.Status == "CheckedIn" && !_context.Admissions.Any(ad => ad.AppointmentId == a.AppointmentId)
                                           join cr in _context.ConsultationRooms on a.ConsultationRoomId equals cr.RoomId
                                           select new AdmissionViewModel
                                           {
                                               AdmissionId = 0,
                                               AppointmentId = a.AppointmentId,
                                               PatientName = a.FullName,
                                               IdNumber = a.IdNumber,
                                               AdmissionDate = null,
                                               Condition = a.Reason,
                                               WardName = cr.RoomName,
                                               FolderStatus = _context.PatientFolder
                                                                .Include(pf => pf.Appointment)
                                                                .Any(pf => pf.Appointment.FullName == a.FullName && pf.Appointment.IdNumber == a.IdNumber)
                                                                ? "Has Folder" : "No Folder",
                                               Status = a.Status
                                           }).ToListAsync();
            //gets the recent folder details
            var folders = _context.PatientFolder
                                        .Include(f => f.Appointment)
                                        .ThenInclude(a => a.User)
                                        .Include(f => f.Appointment)
                                        .ThenInclude(a => a.User.EmergencyContacts)
                                        .GroupBy(f => new { f.Appointment.FullName, f.Appointment.IdNumber })
                                        .Select(g => g.OrderByDescending(f => f.CreatedDate).First()).ToList();
                               
         
            ViewBag.ModelFolderList = folders;


            var allPatients = admittedPatients.Union(checkedInPatients.ToList()).ToList();
            return View(allPatients);
        }

        [Authorize(Roles = "WardAdmin")]
        [HttpPost]
        public async Task<IActionResult> CreateFolder(int appointmentId)
        {
           
            var appointment = await _context.Appointments
                                            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);


            if (appointment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            var folder = new PatientFolder
            {
                AppointmentId = appointment.AppointmentId,
                CreatedBy = user.FullName,
                CreatedDate = DateTime.Now
            };
            _context.PatientFolder.Add(folder);

            await _context.SaveChangesAsync();
            TempData["ToastMessage"] = "Patient folder created successfully.";
            TempData["ToastType"] = "success";
            return RedirectToAction("ViewAdmissions", "WardAdmin");
        }

        //Open Folder 
        [Authorize(Roles ="WardAdmin")]
        [HttpGet]
        public async Task<IActionResult> OpenFolder(int appointmentId)
        {
            var folder = await _context.PatientFolder
                               .Include(f => f.Appointment)
                               .FirstOrDefaultAsync(f => f.AppointmentId == appointmentId);

            if (folder == null) return NotFound();

            return View(folder);
        }

        // GET: Discharge List (only patients with Ready To Be Discharged)
        public async Task<IActionResult> DischargeList()
        {
            var readyPatients = await _context.Appointments
                .Where(a => a.Status == "Ready To Be Discharged")
                .ToListAsync();

            return View(readyPatients);
        }

        // POST: Mark Appointment as Discharged
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Discharge(int appointmentId)
        {
            var appointment = await _context.Appointments
         .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment == null)
            {
                TempData["ToastMessage"] = "Appointment not found.";
                TempData["ToastType"] = "danger";
                return RedirectToAction("DischargeList");
            }

            // Update status
            appointment.Status = "Discharged";

            // Find admission linked to this appointment
            var admission = await _context.Admissions
                .FirstOrDefaultAsync(ad => ad.AppointmentId == appointmentId);

            if (admission != null)
            {
                admission.Status = "Discharged";
            }

            await _context.SaveChangesAsync();

            TempData["ToastMessage"] = $"{appointment.FullName} has been discharged successfully.";
            TempData["ToastType"] = "success";

            return RedirectToAction("DischargeList");
        }

        // GET: Ward/MonitorMovement
        public IActionResult MonitorMovement(PatientMovementFilterViewModel filter)
        {
            var query = _context.PatientMovements
                .Include(m => m.Admission)
                .Include(m => m.Ward)
                .AsQueryable();

            // Filter by patient name
            if (!string.IsNullOrEmpty(filter.PatientName))
            {
                query = query.Where(m => m.Admission.PatientName.Contains(filter.PatientName));
            }

            // Filter by ward
            if (filter.WardId.HasValue)
            {
                query = query.Where(m => m.WardId == filter.WardId.Value);
            }

            // Filter by date range
            if (filter.FromDate.HasValue)
            {
                query = query.Where(m => m.MovementTime >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(m => m.MovementTime <= filter.ToDate.Value);
            }

            var movements = query
                .OrderByDescending(m => m.MovementTime)
                .Select(m => new PatientMovementHistoryViewModel
                {
                    PatientName = m.Admission.PatientName,
                    AdmissionId = m.AdmissionId,
                    WardName = m.Ward.WardName,
                    MovementTime = m.MovementTime,
                    Notes = m.Notes
                })
                .ToList();

            filter.Movements = movements;
            filter.AvailableWards = _context.wards.ToList();

            return View(filter);
        }


    }
}

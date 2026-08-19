using EduCenterManagement.Data;
using EduCenterManagement.Models;
using EduCenterManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduCenterManagement.Pages.GrandManager
{
    [Authorize(Roles = "GrandManager")]
    public class ClassesModel : PageModel
    {
        private readonly EduCenterContext _context;
        private readonly IAuditLogger _auditLogger;

        public ClassesModel(EduCenterContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public List<Class> Classes { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();
        public List<User> Lecturers { get; set; } = new();
        public List<Facility> Facilities { get; set; } = new();
        public List<Room> Rooms { get; set; } = new();
        public List<Shift> Shifts { get; set; } = new();

        public async Task OnGetAsync()
        {
            Classes = await _context.Classes
                .Include(c => c.Subject)
                .Include(c => c.Lecturer)
                .Include(c => c.Facility)
                .Include(c => c.Room)
                .Include(c => c.Shift)
                .Include(c => c.Enrollments)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            Subjects = await _context.Subjects.ToListAsync();
            Lecturers = await _context.Users.Include(u => u.Role).Where(u => u.Role!.RoleName == "Lecturer" && u.IsActive).ToListAsync();
            Facilities = await _context.Facilities.ToListAsync();
            Rooms = await _context.Rooms.Include(r => r.Facility).Where(r => r.Condition == "Open").ToListAsync();
            Shifts = await _context.Shifts.ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateClassAsync(string ClassName, int SubjectId, int LecturerId, int FacilityId, int RoomId, int ShiftId, string DaysOfWeek)
        {
            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1");

            var newClass = new Class
            {
                ClassName = ClassName,
                SubjectId = SubjectId,
                LecturerId = LecturerId,
                FacilityId = FacilityId,
                RoomId = RoomId,
                ShiftId = ShiftId,
                DaysOfWeek = DaysOfWeek,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(3),
                Status = "Active",
                CreatedBy = currentUserId,
                CreatedAt = DateTime.Now
            };

            _context.Classes.Add(newClass);
            await _context.SaveChangesAsync();

            _auditLogger.LogActivity(User.Identity?.Name ?? "GrandManager", "CREATE_CLASS", $"Tạo lớp học mới: {ClassName}");
            TempData["SuccessMessage"] = $"Đã tạo mới lớp học '{ClassName}' thành công!";

            return RedirectToPage();
        }
    }
}

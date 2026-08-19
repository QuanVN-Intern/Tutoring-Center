using EduCenterManagement.Data;
using EduCenterManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduCenterManagement.Pages.Student
{
    [Authorize(Roles = "Student")]
    public class ScheduleModel : PageModel
    {
        private readonly EduCenterContext _context;

        public ScheduleModel(EduCenterContext context)
        {
            _context = context;
        }

        public List<Class> EnrolledClasses { get; set; } = new();
        public List<Attendance> AttendanceHistory { get; set; } = new();

        public async Task OnGetAsync()
        {
            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            var enrolledClassIds = await _context.Enrollments
                .Where(e => e.StudentId == currentUserId && e.Status == "Active")
                .Select(e => e.ClassId)
                .ToListAsync();

            EnrolledClasses = await _context.Classes
                .Include(c => c.Subject)
                .Include(c => c.Lecturer)
                .Include(c => c.Facility)
                .Include(c => c.Room)
                .Include(c => c.Shift)
                .Where(c => enrolledClassIds.Contains(c.ClassId))
                .ToListAsync();

            AttendanceHistory = await _context.Attendance
                .Include(a => a.Session)
                    .ThenInclude(s => s!.Class)
                        .ThenInclude(c => c!.Subject)
                .Include(a => a.Session)
                    .ThenInclude(s => s!.Shift)
                .Include(a => a.Session)
                    .ThenInclude(s => s!.Room)
                .Where(a => a.StudentId == currentUserId)
                .OrderByDescending(a => a.Session!.SessionDate)
                .ToListAsync();
        }
    }
}

using EduCenterManagement.Data;
using EduCenterManagement.Models;
using EduCenterManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduCenterManagement.Pages.Lecturer
{
    [Authorize(Roles = "Lecturer")]
    public class AttendanceModel : PageModel
    {
        private readonly IAttendanceService _attendanceService;
        private readonly EduCenterContext _context;

        public AttendanceModel(IAttendanceService attendanceService, EduCenterContext context)
        {
            _attendanceService = attendanceService;
            _context = context;
        }

        public List<Class> LecturerClasses { get; set; } = new();
        public ClassSession? CurrentSession { get; set; }

        [BindProperty(SupportsGet = true)]
        public int ClassId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime SessionDate { get; set; } = DateTime.Today;

        [BindProperty]
        public List<StudentAttendanceDto> Students { get; set; } = new();

        public async Task OnGetAsync()
        {
            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            LecturerClasses = await _context.Classes
                .Include(c => c.Subject)
                .Include(c => c.Room)
                .Where(c => c.LecturerId == currentUserId && c.Status == "Active")
                .ToListAsync();

            if (ClassId <= 0 && LecturerClasses.Any())
            {
                ClassId = LecturerClasses.First().ClassId;
            }

            if (ClassId > 0)
            {
                CurrentSession = await _attendanceService.GetOrCreateSessionAsync(ClassId, SessionDate);
                if (CurrentSession != null)
                {
                    Students = await _attendanceService.GetAttendanceForSessionAsync(CurrentSession.SessionId);
                }
            }
        }

        public async Task<IActionResult> OnPostSaveAttendanceAsync(int SessionId, int ClassId, DateTime SessionDate)
        {
            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            await _attendanceService.SaveAttendanceAsync(SessionId, Students, currentUserId);
            TempData["SuccessMessage"] = $"Đã lưu bảng điểm danh thành công!";

            return RedirectToPage(new { ClassId, SessionDate = SessionDate.ToString("yyyy-MM-dd") });
        }
    }
}

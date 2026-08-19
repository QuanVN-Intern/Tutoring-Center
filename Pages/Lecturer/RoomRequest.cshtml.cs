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
    public class RoomRequestModel : PageModel
    {
        private readonly IAttendanceService _attendanceService;
        private readonly EduCenterContext _context;

        public RoomRequestModel(IAttendanceService attendanceService, EduCenterContext context)
        {
            _attendanceService = attendanceService;
            _context = context;
        }

        public List<Class> LecturerClasses { get; set; } = new();
        public List<Room> AvailableRooms { get; set; } = new();
        public List<Shift> Shifts { get; set; } = new();
        public List<RoomChangeRequest> MyRequests { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int ClassId { get; set; }

        public async Task OnGetAsync()
        {
            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            LecturerClasses = await _context.Classes
                .Include(c => c.Subject)
                .Include(c => c.Room)
                .Include(c => c.Shift)
                .Where(c => c.LecturerId == currentUserId && c.Status == "Active")
                .ToListAsync();

            if (ClassId <= 0 && LecturerClasses.Any())
            {
                ClassId = LecturerClasses.First().ClassId;
            }

            AvailableRooms = await _context.Rooms.Include(r => r.Facility).Where(r => r.Condition == "Open").ToListAsync();
            Shifts = await _context.Shifts.ToListAsync();

            MyRequests = await _context.RoomChangeRequests
                .Include(r => r.Class)
                .Include(r => r.CurrentRoom)
                .Include(r => r.RequestedRoom)
                .Include(r => r.CurrentShift)
                .Include(r => r.RequestedShift)
                .Where(r => r.LecturerId == currentUserId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostSubmitRequestAsync(int ClassId, int RequestedRoomId, int RequestedShiftId, string Reason)
        {
            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            await _attendanceService.SubmitRoomChangeRequestAsync(currentUserId, ClassId, null, RequestedRoomId, RequestedShiftId, Reason);
            TempData["SuccessMessage"] = "Đã gửi đơn yêu cầu đổi phòng tới Facility Manager thành công!";

            return RedirectToPage();
        }
    }
}

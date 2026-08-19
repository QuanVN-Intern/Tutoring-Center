using EduCenterManagement.Data;
using EduCenterManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduCenterManagement.Pages.Lecturer
{
    [Authorize(Roles = "Lecturer")]
    public class ScheduleModel : PageModel
    {
        private readonly EduCenterContext _context;

        public ScheduleModel(EduCenterContext context)
        {
            _context = context;
        }

        public List<Class> AssignedClasses { get; set; } = new();

        public async Task OnGetAsync()
        {
            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            AssignedClasses = await _context.Classes
                .Include(c => c.Subject)
                .Include(c => c.Facility)
                .Include(c => c.Room)
                .Include(c => c.Shift)
                .Where(c => c.LecturerId == currentUserId && c.Status == "Active")
                .ToListAsync();
        }
    }
}

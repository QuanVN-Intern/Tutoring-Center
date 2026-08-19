using EduCenterManagement.Data;
using EduCenterManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduCenterManagement.Pages.Student
{
    [Authorize(Roles = "Student")]
    public class TuitionModel : PageModel
    {
        private readonly EduCenterContext _context;

        public TuitionModel(EduCenterContext context)
        {
            _context = context;
        }

        public List<Tuition> Tuitions { get; set; } = new();

        public async Task OnGetAsync()
        {
            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

            Tuitions = await _context.Tuitions
                .Include(t => t.Class)
                    .ThenInclude(c => c!.Subject)
                .Include(t => t.Class)
                    .ThenInclude(c => c!.Facility)
                .Where(t => t.StudentId == currentUserId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}

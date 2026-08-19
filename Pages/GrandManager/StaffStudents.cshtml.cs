using EduCenterManagement.Data;
using EduCenterManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EduCenterManagement.Pages.GrandManager
{
    [Authorize(Roles = "GrandManager")]
    public class StaffStudentsModel : PageModel
    {
        private readonly EduCenterContext _context;

        public StaffStudentsModel(EduCenterContext context)
        {
            _context = context;
        }

        public List<User> Users { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SelectedRole { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Users.Include(u => u.Role).AsQueryable();

            if (!string.IsNullOrEmpty(SelectedRole))
            {
                query = query.Where(u => u.Role!.RoleName == SelectedRole);
            }
            else
            {
                // Grand manager manages FacilityManager, Lecturer, Student
                query = query.Where(u => u.Role!.RoleName != "Admin");
            }

            Users = await query.OrderBy(u => u.RoleId).ThenBy(u => u.FullName).ToListAsync();
        }
    }
}

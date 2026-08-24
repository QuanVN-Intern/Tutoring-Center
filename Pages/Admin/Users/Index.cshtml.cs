using EduCenterManagement.Data;
using EduCenterManagement.Models;
using EduCenterManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EduCenterManagement.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly EduCenterContext _context;
        private readonly IAuditLogger _auditLogger;

        public IndexModel(EduCenterContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public List<User> UserList { get; set; } = new();
        public List<Role> Roles { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? RoleId { get; set; }

        public async Task OnGetAsync()
        {
            Roles = await _context.Roles.ToListAsync();

            var query = _context.Users.Include(u => u.Role).AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(u => u.FullName.Contains(SearchTerm) || u.Email.Contains(SearchTerm) || (u.PhoneNumber != null && u.PhoneNumber.Contains(SearchTerm)));
            }

            if (RoleId.HasValue && RoleId.Value > 0)
            {
                query = query.Where(u => u.RoleId == RoleId.Value);
            }

            UserList = await query.OrderBy(u => u.RoleId).ThenBy(u => u.FullName).ToListAsync();
        }

        public async Task<IActionResult> OnPostToggleLockAsync(int id)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();

            if (user.Role?.RoleName == "Admin")
            {
                TempData["ErrorMessage"] = "Không thể khóa tài khoản Admin duy nhất của hệ thống!";
                return RedirectToPage();
            }

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            _auditLogger.LogActivity(User.Identity?.Name ?? "Admin", "TOGGLE_USER_LOCK", $"thay đổi trạng thái tài khoản {user.Email} thành: {(user.IsActive ? "Kích hoạt" : "Bị khóa")}");
            TempData["SuccessMessage"] = $"Đã {(user.IsActive ? "kích hoạt" : "khóa")} tài khoản {user.FullName} thành công!";

            return RedirectToPage();
        }
    }
}

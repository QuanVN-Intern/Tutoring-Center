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
    public class EditModel : PageModel
    {
        private readonly EduCenterContext _context;
        private readonly IAuditLogger _auditLogger;

        public EditModel(EduCenterContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public List<Role> Roles { get; set; } = new();

        [BindProperty]
        public EditUserViewModel InputUser { get; set; } = new();

        public class EditUserViewModel
        {
            public int UserId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string? NewPassword { get; set; }
            public string? PhoneNumber { get; set; }
            public int RoleId { get; set; }
            public bool IsActive { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Roles = await _context.Roles.ToListAsync();
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            InputUser = new EditUserViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                RoleId = user.RoleId,
                IsActive = user.IsActive
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Roles = await _context.Roles.ToListAsync();

            var user = await _context.Users.FindAsync(InputUser.UserId);
            if (user == null) return NotFound();

            user.FullName = InputUser.FullName;
            user.PhoneNumber = InputUser.PhoneNumber;
            user.RoleId = InputUser.RoleId;
            user.IsActive = InputUser.IsActive;
            user.UpdatedAt = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(InputUser.NewPassword))
            {
                user.PasswordHash = EduCenterContext.HashPassword(InputUser.NewPassword);
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _auditLogger.LogActivity(User.Identity?.Name ?? "Admin", "EDIT_USER", $"Cập nhật thông tin tài khoản {user.Email}");
            TempData["SuccessMessage"] = $"Đã cập nhật tài khoản {user.FullName} thành công!";

            return RedirectToPage("/Admin/Users/Index");
        }
    }
}

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
    public class CreateModel : PageModel
    {
        private readonly EduCenterContext _context;
        private readonly IAuditLogger _auditLogger;

        public CreateModel(EduCenterContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public List<Role> Roles { get; set; } = new();

        [BindProperty]
        public CreateUserViewModel InputUser { get; set; } = new();

        public class CreateUserViewModel
        {
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string? PhoneNumber { get; set; }
            public int RoleId { get; set; }
        }

        public async Task OnGetAsync()
        {
            Roles = await _context.Roles.ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Roles = await _context.Roles.ToListAsync();

            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == InputUser.Email.ToLower()))
            {
                ModelState.AddModelError("InputUser.Email", "Email này đã tồn tại trên hệ thống!");
                return Page();
            }

            var newUser = new User
            {
                FullName = InputUser.FullName,
                Email = InputUser.Email,
                PasswordHash = EduCenterContext.HashPassword(InputUser.Password),
                PhoneNumber = InputUser.PhoneNumber,
                RoleId = InputUser.RoleId,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            _auditLogger.LogActivity(User.Identity?.Name ?? "Admin", "CREATE_USER", $"Tạo mới tài khoản {newUser.Email} vai trò RoleId #{newUser.RoleId}");
            TempData["SuccessMessage"] = $"Đã thêm mới tài khoản {newUser.FullName} thành công!";

            return RedirectToPage("/Admin/Users/Index");
        }
    }
}

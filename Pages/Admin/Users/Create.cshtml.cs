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
            // Only allow creating non-Admin roles (Admin is unique and fixed)
            Roles = await _context.Roles.Where(r => r.RoleName != "Admin").ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Roles = await _context.Roles.Where(r => r.RoleName != "Admin").ToListAsync();

            var selectedRole = await _context.Roles.FindAsync(InputUser.RoleId);
            if (selectedRole == null || selectedRole.RoleName == "Admin")
            {
                ModelState.AddModelError("InputUser.RoleId", "Không thể tạo tài khoản với vai trò Admin (Hệ thống chỉ có 1 Admin duy nhất)!");
                return Page();
            }

            // Quota Check: Only 1 Grand Manager allowed
            if (selectedRole.RoleName == "GrandManager")
            {
                bool grandManagerExists = await _context.Users.AnyAsync(u => u.Role!.RoleName == "GrandManager");
                if (grandManagerExists)
                {
                    ModelState.AddModelError("InputUser.RoleId", "Hệ thống chỉ được phép có duy nhất 1 Grand Manager!");
                    return Page();
                }
            }

            // Quota Check: Max 1 Facility Manager per Facility
            if (selectedRole.RoleName == "FacilityManager")
            {
                int facilityCount = await _context.Facilities.CountAsync();
                int currentFmCount = await _context.Users.CountAsync(u => u.Role!.RoleName == "FacilityManager");
                if (currentFmCount >= facilityCount)
                {
                    ModelState.AddModelError("InputUser.RoleId", $"Mỗi cơ sở chỉ có tối đa 1 Facility Manager (Hệ thống hiện đã đủ {currentFmCount}/{facilityCount} Facility Manager)!");
                    return Page();
                }
            }

            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == InputUser.Email.ToLower()))
            {
                ModelState.AddModelError("InputUser.Email", "Email này đã tồn tại trên hệ thống!");
                return Page();
            }

            var newUser = new User
            {
                FullName = InputUser.FullName.Trim(),
                Email = InputUser.Email.Trim(),
                PasswordHash = EduCenterContext.HashPassword(InputUser.Password),
                PhoneNumber = InputUser.PhoneNumber?.Trim(),
                RoleId = InputUser.RoleId,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            _auditLogger.LogActivity(User.Identity?.Name ?? "Admin", "CREATE_USER", $"Tạo mới tài khoản {newUser.Email} vai trò {selectedRole.RoleName}");
            TempData["SuccessMessage"] = $"Đã thêm mới tài khoản {newUser.FullName} ({selectedRole.RoleName}) thành công!";

            return RedirectToPage("/Admin/Users/Index");
        }
    }
}

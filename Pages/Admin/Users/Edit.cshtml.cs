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

        public string CurrentRoleName { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return NotFound();

            CurrentRoleName = user.Role?.RoleName ?? "";

            // Populate allowed roles based on current role transition rules
            if (CurrentRoleName == "Admin")
            {
                Roles = await _context.Roles.Where(r => r.RoleName == "Admin").ToListAsync();
            }
            else if (CurrentRoleName == "Student")
            {
                Roles = await _context.Roles.Where(r => r.RoleName == "Student").ToListAsync();
            }
            else
            {
                // Staff roles (Lecturer, FacilityManager, GrandManager) can only transition among staff roles
                Roles = await _context.Roles.Where(r => r.RoleName != "Admin" && r.RoleName != "Student").ToListAsync();
            }

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
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == InputUser.UserId);
            if (user == null) return NotFound();

            CurrentRoleName = user.Role?.RoleName ?? "";

            // Refresh allowed roles for view
            if (CurrentRoleName == "Admin")
            {
                Roles = await _context.Roles.Where(r => r.RoleName == "Admin").ToListAsync();
            }
            else if (CurrentRoleName == "Student")
            {
                Roles = await _context.Roles.Where(r => r.RoleName == "Student").ToListAsync();
            }
            else
            {
                Roles = await _context.Roles.Where(r => r.RoleName != "Admin" && r.RoleName != "Student").ToListAsync();
            }

            var targetRole = await _context.Roles.FindAsync(InputUser.RoleId);
            if (targetRole == null)
            {
                ModelState.AddModelError("InputUser.RoleId", "Vai trò không hợp lệ!");
                return Page();
            }

            // Rule 1: Admin is fixed and unchangeable
            if (CurrentRoleName == "Admin")
            {
                InputUser.RoleId = user.RoleId;
                InputUser.IsActive = true;
            }
            else
            {
                // No role is allowed to change to Admin
                if (targetRole.RoleName == "Admin")
                {
                    ModelState.AddModelError("InputUser.RoleId", "Không được phép chuyển tài khoản sang vai trò Admin!");
                    return Page();
                }

                // Rule 2: Student cannot change to any other role
                if (CurrentRoleName == "Student" && targetRole.RoleName != "Student")
                {
                    ModelState.AddModelError("InputUser.RoleId", "Tài khoản Học viên (Student) không được phép chuyển sang vai trò khác!");
                    return Page();
                }

                // Rule 3: Other roles cannot change to Student
                if (CurrentRoleName != "Student" && targetRole.RoleName == "Student")
                {
                    ModelState.AddModelError("InputUser.RoleId", "Không được phép chuyển tài khoản nhân sự sang Học viên (Student)!");
                    return Page();
                }

                // Rule 4: Only 1 Grand Manager allowed
                if (targetRole.RoleName == "GrandManager" && CurrentRoleName != "GrandManager")
                {
                    bool grandManagerExists = await _context.Users.AnyAsync(u => u.Role!.RoleName == "GrandManager" && u.UserId != user.UserId);
                    if (grandManagerExists)
                    {
                        ModelState.AddModelError("InputUser.RoleId", "Hệ thống chỉ được phép có duy nhất 1 Grand Manager!");
                        return Page();
                    }
                }

                // Rule 5: Max 1 Facility Manager per Facility
                if (targetRole.RoleName == "FacilityManager" && CurrentRoleName != "FacilityManager")
                {
                    int facilityCount = await _context.Facilities.CountAsync();
                    int currentFmCount = await _context.Users.CountAsync(u => u.Role!.RoleName == "FacilityManager");
                    if (currentFmCount >= facilityCount)
                    {
                        ModelState.AddModelError("InputUser.RoleId", $"Mỗi cơ sở chỉ có tối đa 1 Facility Manager (Đã có đủ {currentFmCount}/{facilityCount} Facility Manager cho các cơ sở)!");
                        return Page();
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            user.FullName = InputUser.FullName.Trim();
            user.PhoneNumber = InputUser.PhoneNumber?.Trim();
            user.RoleId = InputUser.RoleId;
            user.IsActive = (CurrentRoleName == "Admin") ? true : InputUser.IsActive;
            user.UpdatedAt = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(InputUser.NewPassword))
            {
                user.PasswordHash = EduCenterContext.HashPassword(InputUser.NewPassword);
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _auditLogger.LogActivity(User.Identity?.Name ?? "Admin", "EDIT_USER", $"Cập nhật thông tin tài khoản {user.Email} (Vai trò: {targetRole.RoleName})");
            TempData["SuccessMessage"] = $"Đã cập nhật tài khoản {user.FullName} ({targetRole.RoleName}) thành công!";

            return RedirectToPage("/Admin/Users/Index");
        }
    }
}

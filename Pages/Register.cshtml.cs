using EduCenterManagement.Data;
using EduCenterManagement.Models;
using EduCenterManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EduCenterManagement.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly EduCenterContext _context;
        private readonly IAuditLogger _auditLogger;

        public RegisterModel(EduCenterContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public List<Role> Roles { get; set; } = new();

        public string? ErrorMessage { get; set; }

        [BindProperty]
        public RegisterInputModel Input { get; set; } = new();

        public class RegisterInputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập Họ và Tên!")]
            [StringLength(100)]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập Email!")]
            [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ!")]
            [StringLength(100)]
            public string Email { get; set; } = string.Empty;

            [StringLength(20)]
            public string? PhoneNumber { get; set; }

            public int RoleId { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập Mật khẩu!")]
            [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự!")]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập lại Mật khẩu xác nhận!")]
            [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp!")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Index");
            }

            Roles = await _context.Roles.Where(r => r.RoleName == "Student" || r.RoleName == "Lecturer").ToListAsync();
            
            var studentRole = Roles.FirstOrDefault(r => r.RoleName == "Student");
            if (studentRole != null)
            {
                Input.RoleId = studentRole.RoleId;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Roles = await _context.Roles.Where(r => r.RoleName == "Student" || r.RoleName == "Lecturer").ToListAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Input.Password != Input.ConfirmPassword)
            {
                ErrorMessage = "Mật khẩu xác nhận không trùng khớp!";
                return Page();
            }

            if (await _context.Users.AnyAsync(u => u.Email.ToLower() == Input.Email.ToLower()))
            {
                ErrorMessage = "Email này đã được đăng ký tài khoản trên hệ thống!";
                return Page();
            }

            var newUser = new User
            {
                FullName = Input.FullName.Trim(),
                Email = Input.Email.Trim(),
                PasswordHash = EduCenterContext.HashPassword(Input.Password),
                PhoneNumber = Input.PhoneNumber?.Trim(),
                RoleId = Input.RoleId,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            _auditLogger.LogActivity(newUser.Email, "REGISTER", $"Đăng ký tài khoản thành công với vai trò RoleId #{newUser.RoleId}");
            TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";

            return RedirectToPage("/Login");
        }
    }
}

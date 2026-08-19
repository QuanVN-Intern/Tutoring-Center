using EduCenterManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace EduCenterManagement.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IAuthService _authService;

        public LoginModel(IAuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToRoleDashboard(User.FindFirstValue(ClaimTypes.Role));
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Vui lòng nhập đầy đủ Email và Mật khẩu!";
                return Page();
            }

            var user = await _authService.ValidateUserAsync(Email, Password);
            if (user == null)
            {
                ErrorMessage = "Tài khoản hoặc mật khẩu không chính xác, hoặc tài khoản đã bị khóa!";
                return Page();
            }

            await _authService.SignInAsync(HttpContext, user);
            return RedirectToRoleDashboard(user.Role?.RoleName);
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await _authService.SignOutAsync(HttpContext);
            return RedirectToPage("/Login");
        }

        private IActionResult RedirectToRoleDashboard(string? role)
        {
            return role switch
            {
                "Admin" => RedirectToPage("/Admin/Dashboard"),
                "GrandManager" => RedirectToPage("/GrandManager/StaffStudents"),
                "FacilityManager" => RedirectToPage("/FacilityManager/RoomMatrix"),
                "Lecturer" => RedirectToPage("/Lecturer/Schedule"),
                "Student" => RedirectToPage("/Student/Schedule"),
                _ => RedirectToPage("/Index")
            };
        }
    }
}

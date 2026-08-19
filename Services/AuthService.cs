using EduCenterManagement.Data;
using EduCenterManagement.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduCenterManagement.Services
{
    public interface IAuthService
    {
        Task<User?> ValidateUserAsync(string email, string password);
        Task SignInAsync(HttpContext httpContext, User user);
        Task SignOutAsync(HttpContext httpContext);
    }

    public class AuthService : IAuthService
    {
        private readonly EduCenterContext _context;
        private readonly IAuditLogger _auditLogger;

        public AuthService(EduCenterContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public async Task<User?> ValidateUserAsync(string email, string password)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);

            if (user == null) return null;

            string hashedPassword = EduCenterContext.HashPassword(password);
            if (user.PasswordHash != hashedPassword) return null;

            return user;
        }

        public async Task SignInAsync(HttpContext httpContext, User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Student")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            _auditLogger.LogActivity(user.Email, "LOGIN", $"Đăng nhập thành công với vai trò {user.Role?.RoleName}");
        }

        public async Task SignOutAsync(HttpContext httpContext)
        {
            var email = httpContext.User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _auditLogger.LogActivity(email, "LOGOUT", "Đăng xuất khỏi hệ thống");
        }
    }
}

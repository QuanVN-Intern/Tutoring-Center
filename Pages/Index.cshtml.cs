using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace EduCenterManagement.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var role = User.FindFirstValue(ClaimTypes.Role);
                return role switch
                {
                    "Admin" => RedirectToPage("/Admin/Dashboard"),
                    "GrandManager" => RedirectToPage("/GrandManager/StaffStudents"),
                    "FacilityManager" => RedirectToPage("/FacilityManager/RoomMatrix"),
                    "Lecturer" => RedirectToPage("/Lecturer/Schedule"),
                    "Student" => RedirectToPage("/Student/Schedule"),
                    _ => Page()
                };
            }
            return RedirectToPage("/Login");
        }
    }
}

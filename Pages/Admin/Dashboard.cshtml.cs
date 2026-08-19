using EduCenterManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduCenterManagement.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly IDashboardService _dashboardService;

        public DashboardModel(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public AdminDashboardDto Stats { get; set; } = new();

        public async Task OnGetAsync()
        {
            Stats = await _dashboardService.GetAdminDashboardStatsAsync();
        }
    }
}

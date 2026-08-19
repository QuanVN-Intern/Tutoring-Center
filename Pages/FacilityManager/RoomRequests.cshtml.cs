using EduCenterManagement.Models;
using EduCenterManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace EduCenterManagement.Pages.FacilityManager
{
    [Authorize(Roles = "FacilityManager")]
    public class RoomRequestsModel : PageModel
    {
        private readonly IRoomMatrixService _roomMatrixService;

        public RoomRequestsModel(IRoomMatrixService roomMatrixService)
        {
            _roomMatrixService = roomMatrixService;
        }

        public List<RoomChangeRequest> Requests { get; set; } = new();

        public async Task OnGetAsync()
        {
            Requests = await _roomMatrixService.GetPendingRoomRequestsAsync();
        }

        public async Task<IActionResult> OnPostReviewAsync(int RequestId, bool Approve, string? ReviewNote)
        {
            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1");

            bool success = await _roomMatrixService.ReviewRoomRequestAsync(RequestId, Approve, ReviewNote, currentUserId);
            if (success)
            {
                TempData["SuccessMessage"] = $"Đã {(Approve ? "phê duyệt" : "từ chối")} đơn yêu cầu #{RequestId} thành công!";
            }

            return RedirectToPage();
        }
    }
}

using EduCenterManagement.Data;
using EduCenterManagement.Models;
using EduCenterManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EduCenterManagement.Pages.FacilityManager
{
    [Authorize(Roles = "FacilityManager")]
    public class RoomMatrixModel : PageModel
    {
        private readonly IRoomMatrixService _roomMatrixService;
        private readonly EduCenterContext _context;

        public RoomMatrixModel(IRoomMatrixService roomMatrixService, EduCenterContext context)
        {
            _roomMatrixService = roomMatrixService;
            _context = context;
        }

        public List<Facility> Facilities { get; set; } = new();
        public FacilityMatrixDto MatrixData { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int FacilityId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime TargetDate { get; set; } = DateTime.Today;

        public async Task OnGetAsync()
        {
            Facilities = await _context.Facilities.ToListAsync();

            if (FacilityId <= 0 && Facilities.Any())
            {
                FacilityId = Facilities.First().FacilityId;
            }

            MatrixData = await _roomMatrixService.GetFacilityRoomMatrixAsync(FacilityId, TargetDate);
        }

        public async Task<IActionResult> OnPostToggleConditionAsync(int roomId, int facilityId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room != null)
            {
                string targetCondition = room.Condition == "Open" ? "Closed" : "Open";
                await _roomMatrixService.ToggleRoomConditionAsync(roomId, targetCondition, User.Identity?.Name ?? "FacilityManager");
                TempData["SuccessMessage"] = $"Đã chuyển phòng {room.RoomNumber} sang trạng thái: {targetCondition}";
            }

            return RedirectToPage(new { FacilityId = facilityId, TargetDate });
        }
    }
}

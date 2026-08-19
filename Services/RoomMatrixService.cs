using EduCenterManagement.Data;
using EduCenterManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace EduCenterManagement.Services
{
    public class RoomSlotStatusDto
    {
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string Condition { get; set; } = "Open"; // Open / Closed
        public int ShiftId { get; set; }
        public string ShiftName { get; set; } = string.Empty;
        public bool IsOccupied { get; set; }
        public string? ClassName { get; set; }
        public string? SubjectName { get; set; }
        public string? LecturerName { get; set; }
    }

    public class FacilityMatrixDto
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; } = string.Empty;
        public List<Room> Rooms { get; set; } = new();
        public List<Shift> Shifts { get; set; } = new();
        public List<RoomSlotStatusDto> Slots { get; set; } = new();
    }

    public interface IRoomMatrixService
    {
        Task<FacilityMatrixDto> GetFacilityRoomMatrixAsync(int facilityId, DateTime targetDate);
        Task<bool> ToggleRoomConditionAsync(int roomId, string newCondition, string userEmail);
        Task<List<RoomChangeRequest>> GetPendingRoomRequestsAsync(int? facilityId = null);
        Task<bool> ReviewRoomRequestAsync(int requestId, bool approve, string? reviewNote, int reviewerUserId);
    }

    public class RoomMatrixService : IRoomMatrixService
    {
        private readonly EduCenterContext _context;
        private readonly IAuditLogger _auditLogger;

        public RoomMatrixService(EduCenterContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public async Task<FacilityMatrixDto> GetFacilityRoomMatrixAsync(int facilityId, DateTime targetDate)
        {
            var facility = await _context.Facilities
                .FirstOrDefaultAsync(f => f.FacilityId == facilityId);

            if (facility == null)
            {
                facility = await _context.Facilities.FirstAsync();
                facilityId = facility.FacilityId;
            }

            var rooms = await _context.Rooms
                .Where(r => r.FacilityId == facilityId)
                .OrderBy(r => r.RoomNumber)
                .ToListAsync();

            var shifts = await _context.Shifts.OrderBy(s => s.ShiftId).ToListAsync();

            var result = new FacilityMatrixDto
            {
                FacilityId = facility.FacilityId,
                FacilityName = facility.FacilityName,
                Rooms = rooms,
                Shifts = shifts
            };

            // Fetch active classes in this facility
            var activeClasses = await _context.Classes
                .Include(c => c.Subject)
                .Include(c => c.Lecturer)
                .Where(c => c.FacilityId == facilityId && c.Status == "Active")
                .ToListAsync();

            // Fetch specific sessions on target date if any
            var targetSessions = await _context.ClassSessions
                .Include(cs => cs.Class)
                    .ThenInclude(c => c!.Subject)
                .Include(cs => cs.Class)
                    .ThenInclude(c => c!.Lecturer)
                .Where(cs => cs.SessionDate.Date == targetDate.Date && cs.Class!.FacilityId == facilityId && cs.Status != "Cancelled")
                .ToListAsync();

            foreach (var room in rooms)
            {
                foreach (var shift in shifts)
                {
                    var slot = new RoomSlotStatusDto
                    {
                        RoomId = room.RoomId,
                        RoomNumber = room.RoomNumber,
                        Capacity = room.Capacity ?? 30,
                        Condition = room.Condition,
                        ShiftId = shift.ShiftId,
                        ShiftName = shift.ShiftName,
                        IsOccupied = false
                    };

                    if (room.Condition == "Closed")
                    {
                        slot.IsOccupied = true;
                        slot.ClassName = "BẢO TRÌ (Closed)";
                    }
                    else
                    {
                        // Check if there is a session on this specific date
                        var sessionMatch = targetSessions.FirstOrDefault(cs => cs.RoomId == room.RoomId && cs.ShiftId == shift.ShiftId);
                        if (sessionMatch != null)
                        {
                            slot.IsOccupied = true;
                            slot.ClassName = sessionMatch.Class?.ClassName;
                            slot.SubjectName = sessionMatch.Class?.Subject?.SubjectName;
                            slot.LecturerName = sessionMatch.Class?.Lecturer?.FullName;
                        }
                        else
                        {
                            // Fallback check default class schedule by DayOfWeek
                            string dayOfWeekStr = ((int)targetDate.DayOfWeek == 0 ? "8" : ((int)targetDate.DayOfWeek + 1).ToString()); // 2=Mon, ..., 8=Sun
                            var defaultClassMatch = activeClasses.FirstOrDefault(c => c.RoomId == room.RoomId
                                && c.ShiftId == shift.ShiftId
                                && c.DaysOfWeek.Contains(dayOfWeekStr));

                            if (defaultClassMatch != null)
                            {
                                slot.IsOccupied = true;
                                slot.ClassName = defaultClassMatch.ClassName;
                                slot.SubjectName = defaultClassMatch.Subject?.SubjectName;
                                slot.LecturerName = defaultClassMatch.Lecturer?.FullName;
                            }
                        }
                    }

                    result.Slots.Add(slot);
                }
            }

            return result;
        }

        public async Task<bool> ToggleRoomConditionAsync(int roomId, string newCondition, string userEmail)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return false;

            room.Condition = newCondition;
            await _context.SaveChangesAsync();

            _auditLogger.LogActivity(userEmail, "TOGGLE_ROOM_CONDITION", $"Cập nhật phòng {room.RoomNumber} sang trạng thái: {newCondition}");
            return true;
        }

        public async Task<List<RoomChangeRequest>> GetPendingRoomRequestsAsync(int? facilityId = null)
        {
            var query = _context.RoomChangeRequests
                .Include(r => r.Lecturer)
                .Include(r => r.Class)
                    .ThenInclude(c => c!.Facility)
                .Include(r => r.Session)
                .Include(r => r.CurrentRoom)
                .Include(r => r.RequestedRoom)
                .Include(r => r.CurrentShift)
                .Include(r => r.RequestedShift)
                .AsQueryable();

            if (facilityId.HasValue && facilityId.Value > 0)
            {
                query = query.Where(r => r.Class!.FacilityId == facilityId.Value);
            }

            return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        }

        public async Task<bool> ReviewRoomRequestAsync(int requestId, bool approve, string? reviewNote, int reviewerUserId)
        {
            var req = await _context.RoomChangeRequests
                .Include(r => r.Class)
                .Include(r => r.Session)
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (req == null) return false;

            req.Status = approve ? "Approved" : "Rejected";
            req.ReviewedBy = reviewerUserId;
            req.ReviewedAt = DateTime.Now;
            req.ReviewNote = reviewNote;

            if (approve)
            {
                // If specific session request, update session
                if (req.SessionId.HasValue && req.Session != null)
                {
                    if (req.RequestedRoomId.HasValue) req.Session.RoomId = req.RequestedRoomId.Value;
                    if (req.RequestedShiftId.HasValue) req.Session.ShiftId = req.RequestedShiftId.Value;
                    _context.ClassSessions.Update(req.Session);
                }
                else if (req.Class != null)
                {
                    // Update default room/shift for entire class
                    if (req.RequestedRoomId.HasValue) req.Class.RoomId = req.RequestedRoomId.Value;
                    if (req.RequestedShiftId.HasValue) req.Class.ShiftId = req.RequestedShiftId.Value;
                    _context.Classes.Update(req.Class);
                }
            }

            await _context.SaveChangesAsync();

            var reviewer = await _context.Users.FindAsync(reviewerUserId);
            _auditLogger.LogActivity(reviewer?.Email ?? "FacilityManager", "REVIEW_ROOM_REQUEST", $"{(approve ? "Phê duyệt" : "Từ chối")} yêu cầu đổi phòng #{requestId}. Note: {reviewNote}");

            return true;
        }
    }
}

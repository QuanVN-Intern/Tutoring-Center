using EduCenterManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace EduCenterManagement.Services
{
    public class RoomFacilityStatDto
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; } = string.Empty;
        public int ActiveRooms { get; set; }
        public int MaintenanceRooms { get; set; }
        public int TotalRooms { get; set; }
    }

    public class ShiftFillRateDto
    {
        public int ShiftId { get; set; }
        public string ShiftName { get; set; } = string.Empty;
        public string Timeline { get; set; } = string.Empty;
        public int OccupiedSlots { get; set; }
        public int TotalAvailableSlots { get; set; }
        public double FillRatePercent { get; set; }
    }

    public class RevenueBySubjectDto
    {
        public string SubjectName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int TotalClasses { get; set; }
    }

    public class RevenueByFacilityDto
    {
        public string FacilityName { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public int TotalClasses { get; set; }
    }

    public class AdminDashboardDto
    {
        public int TotalStudents { get; set; }
        public int TotalLecturers { get; set; }
        public int TotalClasses { get; set; }
        public int TotalActiveRooms { get; set; }
        public int TotalMaintenanceRooms { get; set; }
        public decimal TotalSystemRevenue { get; set; }

        public List<RoomFacilityStatDto> FacilityRoomStats { get; set; } = new();
        public List<ShiftFillRateDto> ShiftFillRates { get; set; } = new();
        public List<RevenueBySubjectDto> RevenueBySubject { get; set; } = new();
        public List<RevenueByFacilityDto> RevenueByFacility { get; set; } = new();
    }

    public interface IDashboardService
    {
        Task<AdminDashboardDto> GetAdminDashboardStatsAsync();
    }

    public class DashboardService : IDashboardService
    {
        private readonly EduCenterContext _context;

        public DashboardService(EduCenterContext context)
        {
            _context = context;
        }

        public async Task<AdminDashboardDto> GetAdminDashboardStatsAsync()
        {
            var dto = new AdminDashboardDto();

            // Total users
            dto.TotalStudents = await _context.Users.CountAsync(u => u.Role!.RoleName == "Student" && u.IsActive);
            dto.TotalLecturers = await _context.Users.CountAsync(u => u.Role!.RoleName == "Lecturer" && u.IsActive);
            dto.TotalClasses = await _context.Classes.CountAsync(c => c.Status == "Active");

            // Facility & Room Stats
            var facilities = await _context.Facilities.Include(f => f.Rooms).ToListAsync();
            foreach (var fac in facilities)
            {
                int active = fac.Rooms.Count(r => r.Condition == "Open");
                int maintenance = fac.Rooms.Count(r => r.Condition == "Closed");

                dto.FacilityRoomStats.Add(new RoomFacilityStatDto
                {
                    FacilityId = fac.FacilityId,
                    FacilityName = fac.FacilityName,
                    ActiveRooms = active,
                    MaintenanceRooms = maintenance,
                    TotalRooms = fac.Rooms.Count
                });
            }

            dto.TotalActiveRooms = dto.FacilityRoomStats.Sum(f => f.ActiveRooms);
            dto.TotalMaintenanceRooms = dto.FacilityRoomStats.Sum(f => f.MaintenanceRooms);

            // Shifts fill rates
            var shifts = await _context.Shifts.ToListAsync();
            int openRoomsCount = Math.Max(1, dto.TotalActiveRooms);

            foreach (var s in shifts)
            {
                int occupiedClasses = await _context.Classes
                    .CountAsync(c => c.ShiftId == s.ShiftId && c.Status == "Active");

                string timeline = $"{s.StartTime:hh\\:mm}–{s.EndTime:hh\\:mm}";
                double percent = Math.Min(100.0, Math.Round((double)occupiedClasses / openRoomsCount * 100, 1));

                dto.ShiftFillRates.Add(new ShiftFillRateDto
                {
                    ShiftId = s.ShiftId,
                    ShiftName = s.ShiftName,
                    Timeline = timeline,
                    OccupiedSlots = occupiedClasses,
                    TotalAvailableSlots = openRoomsCount,
                    FillRatePercent = percent
                });
            }

            // Revenue reports
            var paidTuitions = await _context.Tuitions
                .Include(t => t.Class)
                .ThenInclude(c => c!.Subject)
                .Include(t => t.Class)
                .ThenInclude(c => c!.Facility)
                .Where(t => t.PaymentStatus == "Paid")
                .ToListAsync();

            dto.TotalSystemRevenue = paidTuitions.Sum(t => t.TotalAmount);

            // By Subject
            var subjects = await _context.Subjects.ToListAsync();
            foreach (var subj in subjects)
            {
                var subjTuitions = paidTuitions.Where(t => t.Class?.SubjectId == subj.SubjectId);
                int classCount = await _context.Classes.CountAsync(c => c.SubjectId == subj.SubjectId);

                dto.RevenueBySubject.Add(new RevenueBySubjectDto
                {
                    SubjectName = subj.SubjectName,
                    TotalRevenue = subjTuitions.Sum(t => t.TotalAmount),
                    TotalClasses = classCount
                });
            }

            // By Facility
            foreach (var fac in facilities)
            {
                var facTuitions = paidTuitions.Where(t => t.Class?.FacilityId == fac.FacilityId);
                int classCount = await _context.Classes.CountAsync(c => c.FacilityId == fac.FacilityId);

                dto.RevenueByFacility.Add(new RevenueByFacilityDto
                {
                    FacilityName = fac.FacilityName,
                    TotalRevenue = facTuitions.Sum(t => t.TotalAmount),
                    TotalClasses = classCount
                });
            }

            return dto;
        }
    }
}

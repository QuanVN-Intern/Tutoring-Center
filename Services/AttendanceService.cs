using EduCenterManagement.Data;
using EduCenterManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace EduCenterManagement.Services
{
    public class StudentAttendanceDto
    {
        public int StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = "Present"; // Present / ExcusedAbsent / UnexcusedAbsent
        public string? Note { get; set; }
    }

    public interface IAttendanceService
    {
        Task<ClassSession?> GetOrCreateSessionAsync(int classId, DateTime sessionDate);
        Task<List<StudentAttendanceDto>> GetAttendanceForSessionAsync(int sessionId);
        Task SaveAttendanceAsync(int sessionId, List<StudentAttendanceDto> attendanceList, int recordedByUserId);
        Task<RoomChangeRequest> SubmitRoomChangeRequestAsync(int lecturerUserId, int classId, int? sessionId, int requestedRoomId, int requestedShiftId, string reason);
    }

    public class AttendanceService : IAttendanceService
    {
        private readonly EduCenterContext _context;
        private readonly IAuditLogger _auditLogger;

        public AttendanceService(EduCenterContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public async Task<ClassSession?> GetOrCreateSessionAsync(int classId, DateTime sessionDate)
        {
            var targetClass = await _context.Classes.FindAsync(classId);
            if (targetClass == null) return null;

            var existingSession = await _context.ClassSessions
                .Include(cs => cs.Class)
                    .ThenInclude(c => c!.Subject)
                .Include(cs => cs.Room)
                .Include(cs => cs.Shift)
                .FirstOrDefaultAsync(cs => cs.ClassId == classId && cs.SessionDate.Date == sessionDate.Date);

            if (existingSession != null) return existingSession;

            // Create session if not exists
            var newSession = new ClassSession
            {
                ClassId = classId,
                SessionDate = sessionDate.Date,
                RoomId = targetClass.RoomId,
                ShiftId = targetClass.ShiftId,
                Status = "Scheduled"
            };

            _context.ClassSessions.Add(newSession);
            await _context.SaveChangesAsync();

            return await _context.ClassSessions
                .Include(cs => cs.Class)
                    .ThenInclude(c => c!.Subject)
                .Include(cs => cs.Room)
                .Include(cs => cs.Shift)
                .FirstAsync(cs => cs.SessionId == newSession.SessionId);
        }

        public async Task<List<StudentAttendanceDto>> GetAttendanceForSessionAsync(int sessionId)
        {
            var session = await _context.ClassSessions
                .Include(cs => cs.Class)
                    .ThenInclude(c => c!.Enrollments)
                        .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(cs => cs.SessionId == sessionId);

            if (session?.Class == null) return new List<StudentAttendanceDto>();

            var existingAttendances = await _context.Attendance
                .Where(a => a.SessionId == sessionId)
                .ToListAsync();

            var enrolledStudents = session.Class.Enrollments
                .Where(e => e.Status == "Active")
                .Select(e => e.Student)
                .Where(s => s != null && s.IsActive)
                .ToList();

            var result = new List<StudentAttendanceDto>();

            foreach (var student in enrolledStudents)
            {
                var att = existingAttendances.FirstOrDefault(a => a.StudentId == student!.UserId);
                result.Add(new StudentAttendanceDto
                {
                    StudentId = student!.UserId,
                    FullName = student.FullName,
                    Email = student.Email,
                    Status = att?.Status ?? "Present",
                    Note = att?.Note
                });
            }

            return result;
        }

        public async Task SaveAttendanceAsync(int sessionId, List<StudentAttendanceDto> attendanceList, int recordedByUserId)
        {
            var session = await _context.ClassSessions.FindAsync(sessionId);
            if (session == null) return;

            var existingAttendances = await _context.Attendance
                .Where(a => a.SessionId == sessionId)
                .ToListAsync();

            foreach (var dto in attendanceList)
            {
                var att = existingAttendances.FirstOrDefault(a => a.StudentId == dto.StudentId);
                if (att == null)
                {
                    _context.Attendance.Add(new Attendance
                    {
                        SessionId = sessionId,
                        StudentId = dto.StudentId,
                        Status = dto.Status,
                        Note = dto.Note,
                        RecordedBy = recordedByUserId,
                        RecordedAt = DateTime.Now
                    });
                }
                else
                {
                    att.Status = dto.Status;
                    att.Note = dto.Note;
                    att.RecordedBy = recordedByUserId;
                    att.UpdatedAt = DateTime.Now;
                    _context.Attendance.Update(att);
                }
            }

            session.Status = "Completed";
            _context.ClassSessions.Update(session);

            await _context.SaveChangesAsync();

            var recorder = await _context.Users.FindAsync(recordedByUserId);
            _auditLogger.LogActivity(recorder?.Email ?? "Lecturer", "SAVE_ATTENDANCE", $"Đã lưu điểm danh cho buổi học #{sessionId} ngày {session.SessionDate:dd/MM/yyyy}");
        }

        public async Task<RoomChangeRequest> SubmitRoomChangeRequestAsync(int lecturerUserId, int classId, int? sessionId, int requestedRoomId, int requestedShiftId, string reason)
        {
            var targetClass = await _context.Classes.FirstAsync(c => c.ClassId == classId);

            var req = new RoomChangeRequest
            {
                LecturerId = lecturerUserId,
                ClassId = classId,
                SessionId = sessionId,
                CurrentRoomId = targetClass.RoomId,
                RequestedRoomId = requestedRoomId,
                CurrentShiftId = targetClass.ShiftId,
                RequestedShiftId = requestedShiftId,
                Reason = reason,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.RoomChangeRequests.Add(req);
            await _context.SaveChangesAsync();

            var lecturer = await _context.Users.FindAsync(lecturerUserId);
            _auditLogger.LogActivity(lecturer?.Email ?? "Lecturer", "SUBMIT_ROOM_REQUEST", $"Gửi yêu cầu đổi sang phòng #{requestedRoomId}, Ca #{requestedShiftId}. Lý do: {reason}");

            return req;
        }
    }
}

using EduCenterManagement.Data;
using EduCenterManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace EduCenterManagement.Services
{
    public interface ITuitionService
    {
        Task CalculateTuitionForClassAsync(int classId, DateTime periodFrom, DateTime periodTo);
        Task<List<Tuition>> GetTuitionReportAsync(int? facilityId = null, string? paymentStatus = null);
        Task<Payment?> RecordPaymentAsync(int tuitionId, decimal amountPaid, string paymentMethod, int receivedByUserId);
        Task<Tuition?> GetTuitionDetailAsync(int tuitionId);
    }

    public class TuitionService : ITuitionService
    {
        private readonly EduCenterContext _context;
        private readonly IAuditLogger _auditLogger;

        public TuitionService(EduCenterContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public async Task CalculateTuitionForClassAsync(int classId, DateTime periodFrom, DateTime periodTo)
        {
            var targetClass = await _context.Classes
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.ClassId == classId);

            if (targetClass == null) return;

            // Get default rate from settings
            decimal rate = 200000;
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == "TUITION_PER_SESSION");
            if (setting != null && decimal.TryParse(setting.SettingValue, out decimal parsedRate))
            {
                rate = parsedRate;
            }

            var activeStudents = targetClass.Enrollments
                .Where(e => e.Status == "Active")
                .Select(e => e.StudentId)
                .ToList();

            foreach (var studentId in activeStudents)
            {
                // Count Present sessions
                int presentCount = await _context.Attendance
                    .Include(a => a.Session)
                    .Where(a => a.StudentId == studentId
                                && a.Session!.ClassId == classId
                                && a.Session.SessionDate >= periodFrom.Date
                                && a.Session.SessionDate <= periodTo.Date
                                && a.Status == "Present")
                    .CountAsync();

                var existingTuition = await _context.Tuitions
                    .FirstOrDefaultAsync(t => t.StudentId == studentId
                                              && t.ClassId == classId
                                              && t.PeriodFrom.Date == periodFrom.Date
                                              && t.PeriodTo.Date == periodTo.Date);

                if (existingTuition == null)
                {
                    var newTuition = new Tuition
                    {
                        StudentId = studentId,
                        ClassId = classId,
                        PeriodFrom = periodFrom.Date,
                        PeriodTo = periodTo.Date,
                        SessionsAttended = presentCount,
                        AmountPerSession = rate,
                        PaymentStatus = "Unpaid",
                        DueDate = DateTime.Today.AddDays(7),
                        CreatedAt = DateTime.Now
                    };
                    _context.Tuitions.Add(newTuition);
                }
                else
                {
                    existingTuition.SessionsAttended = presentCount;
                    existingTuition.AmountPerSession = rate;
                    _context.Tuitions.Update(existingTuition);
                }
            }

            await _context.SaveChangesAsync();
            _auditLogger.LogActivity("GrandManager", "CALCULATE_TUITION", $"Tính học phí lớp {targetClass.ClassName} từ {periodFrom:dd/MM/yyyy} đến {periodTo:dd/MM/yyyy}");
        }

        public async Task<List<Tuition>> GetTuitionReportAsync(int? facilityId = null, string? paymentStatus = null)
        {
            var query = _context.Tuitions
                .Include(t => t.Student)
                .Include(t => t.Class)
                    .ThenInclude(c => c!.Subject)
                .Include(t => t.Class)
                    .ThenInclude(c => c!.Facility)
                .Include(t => t.Payments)
                .AsQueryable();

            if (facilityId.HasValue && facilityId.Value > 0)
            {
                query = query.Where(t => t.Class!.FacilityId == facilityId.Value);
            }

            if (!string.IsNullOrEmpty(paymentStatus))
            {
                query = query.Where(t => t.PaymentStatus == paymentStatus);
            }

            return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        }

        public async Task<Payment?> RecordPaymentAsync(int tuitionId, decimal amountPaid, string paymentMethod, int receivedByUserId)
        {
            var tuition = await _context.Tuitions.Include(t => t.Student).FirstOrDefaultAsync(t => t.TuitionId == tuitionId);
            if (tuition == null) return null;

            string receiptNo = $"REC-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

            var payment = new Payment
            {
                TuitionId = tuitionId,
                AmountPaid = amountPaid,
                PaymentDate = DateTime.Now,
                PaymentMethod = paymentMethod,
                ReceivedBy = receivedByUserId,
                ReceiptNumber = receiptNo
            };

            _context.Payments.Add(payment);

            // Update tuition status if full amount paid
            var existingPaymentsSum = await _context.Payments
                .Where(p => p.TuitionId == tuitionId)
                .SumAsync(p => (decimal?)p.AmountPaid) ?? 0;

            if ((existingPaymentsSum + amountPaid) >= tuition.TotalAmount)
            {
                tuition.PaymentStatus = "Paid";
                _context.Tuitions.Update(tuition);
            }

            await _context.SaveChangesAsync();

            var receiver = await _context.Users.FindAsync(receivedByUserId);
            _auditLogger.LogActivity(receiver?.Email ?? "GrandManager", "RECORD_PAYMENT", $"Thu học phí {amountPaid:N0} VNĐ cho học viên {tuition.Student?.FullName} (Biên lai: {receiptNo})");

            return payment;
        }

        public async Task<Tuition?> GetTuitionDetailAsync(int tuitionId)
        {
            return await _context.Tuitions
                .Include(t => t.Student)
                .Include(t => t.Class)
                    .ThenInclude(c => c!.Subject)
                .Include(t => t.Class)
                    .ThenInclude(c => c!.Facility)
                .Include(t => t.Class)
                    .ThenInclude(c => c!.Lecturer)
                .Include(t => t.Payments)
                    .ThenInclude(p => p.Receiver)
                .FirstOrDefaultAsync(t => t.TuitionId == tuitionId);
        }
    }
}

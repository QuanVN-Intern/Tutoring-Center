using EduCenterManagement.Data;
using EduCenterManagement.Models;
using EduCenterManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EduCenterManagement.Pages.GrandManager
{
    [Authorize(Roles = "GrandManager")]
    public class TuitionModel : PageModel
    {
        private readonly ITuitionService _tuitionService;
        private readonly EduCenterContext _context;

        public TuitionModel(ITuitionService tuitionService, EduCenterContext context)
        {
            _tuitionService = tuitionService;
            _context = context;
        }

        public List<Tuition> Tuitions { get; set; } = new();
        public List<Facility> Facilities { get; set; } = new();
        public List<Class> Classes { get; set; } = new();
        public Tuition? ReceiptTuitionDetail { get; set; }
        public decimal TotalUnpaidAmount { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FacilityId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? PaymentStatus { get; set; }

        public async Task OnGetAsync()
        {
            Facilities = await _context.Facilities.ToListAsync();
            Classes = await _context.Classes.Include(c => c.Subject).Include(c => c.Facility).Where(c => c.Status == "Active").ToListAsync();

            Tuitions = await _tuitionService.GetTuitionReportAsync(FacilityId, PaymentStatus);
            TotalUnpaidAmount = Tuitions.Where(t => t.PaymentStatus == "Unpaid").Sum(t => t.TotalAmount);
        }

        public async Task<IActionResult> OnGetViewReceiptAsync(int id)
        {
            await OnGetAsync();
            ReceiptTuitionDetail = await _tuitionService.GetTuitionDetailAsync(id);
            return Page();
        }

        public async Task<IActionResult> OnPostCalculateTuitionAsync(int ClassId, DateTime PeriodFrom, DateTime PeriodTo)
        {
            await _tuitionService.CalculateTuitionForClassAsync(ClassId, PeriodFrom, PeriodTo);
            TempData["SuccessMessage"] = "Đã tính toán học phí tự động dựa trên số buổi có mặt thành công!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRecordPaymentAsync(int TuitionId, decimal AmountPaid, string PaymentMethod)
        {
            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1");

            var payment = await _tuitionService.RecordPaymentAsync(TuitionId, AmountPaid, PaymentMethod, currentUserId);
            if (payment != null)
            {
                TempData["SuccessMessage"] = $"Ghi nhận thu học phí thành công! Mã biên lai: {payment.ReceiptNumber}";
            }

            return RedirectToPage("/GrandManager/Tuition", "ViewReceipt", new { id = TuitionId });
        }
    }
}

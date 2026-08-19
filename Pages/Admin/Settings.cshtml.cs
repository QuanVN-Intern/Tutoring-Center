using EduCenterManagement.Data;
using EduCenterManagement.Models;
using EduCenterManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EduCenterManagement.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class SettingsModel : PageModel
    {
        private readonly EduCenterContext _context;
        private readonly IAuditLogger _auditLogger;

        public SettingsModel(EduCenterContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public string TuitionPerSession { get; set; } = "200000";
        public List<Subject> Subjects { get; set; } = new();
        public List<Facility> Facilities { get; set; } = new();

        public async Task OnGetAsync()
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == "TUITION_PER_SESSION");
            if (setting != null) TuitionPerSession = setting.SettingValue;

            Subjects = await _context.Subjects.ToListAsync();
            Facilities = await _context.Facilities.Include(f => f.Rooms).ToListAsync();
        }

        public async Task<IActionResult> OnPostSaveSettingAsync(string TuitionPerSession)
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.SettingKey == "TUITION_PER_SESSION");
            if (setting == null)
            {
                setting = new SystemSetting
                {
                    SettingKey = "TUITION_PER_SESSION",
                    SettingValue = TuitionPerSession,
                    Description = "Học phí mặc định mỗi buổi học (VNĐ)"
                };
                _context.SystemSettings.Add(setting);
            }
            else
            {
                setting.SettingValue = TuitionPerSession;
                _context.SystemSettings.Update(setting);
            }

            await _context.SaveChangesAsync();
            _auditLogger.LogActivity(User.Identity?.Name ?? "Admin", "UPDATE_SETTING", $"Đã thay đổi mức học phí mặc định thành {TuitionPerSession} VNĐ");
            TempData["SuccessMessage"] = "Lưu định mức học phí mới thành công!";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddSubjectAsync(string NewSubjectName)
        {
            if (string.IsNullOrWhiteSpace(NewSubjectName)) return RedirectToPage();

            if (await _context.Subjects.AnyAsync(s => s.SubjectName.ToLower() == NewSubjectName.ToLower()))
            {
                TempData["ErrorMessage"] = "Môn học này đã tồn tại trong danh mục!";
                return RedirectToPage();
            }

            _context.Subjects.Add(new Subject { SubjectName = NewSubjectName.Trim() });
            await _context.SaveChangesAsync();

            _auditLogger.LogActivity(User.Identity?.Name ?? "Admin", "ADD_SUBJECT", $"Thêm môn học mới: {NewSubjectName}");
            TempData["SuccessMessage"] = $"Đã thêm môn học '{NewSubjectName}' thành công!";

            return RedirectToPage();
        }
    }
}

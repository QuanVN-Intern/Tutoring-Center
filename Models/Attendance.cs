using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduCenterManagement.Models
{
    [Table("Attendance")]
    public class Attendance
    {
        [Key]
        public int AttendanceId { get; set; }

        public int SessionId { get; set; }

        public int StudentId { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Present"; // Present / ExcusedAbsent / UnexcusedAbsent

        [StringLength(255)]
        public string? Note { get; set; }

        public int RecordedBy { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("SessionId")]
        public virtual ClassSession? Session { get; set; }

        [ForeignKey("StudentId")]
        public virtual User? Student { get; set; }

        [ForeignKey("RecordedBy")]
        public virtual User? Recorder { get; set; }
    }
}

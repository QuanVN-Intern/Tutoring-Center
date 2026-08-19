using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduCenterManagement.Models
{
    [Table("Classes")]
    public class Class
    {
        [Key]
        public int ClassId { get; set; }

        [Required]
        [StringLength(100)]
        public string ClassName { get; set; } = string.Empty;

        public int SubjectId { get; set; }

        public int LecturerId { get; set; }

        public int FacilityId { get; set; }

        public int RoomId { get; set; }

        public int ShiftId { get; set; }

        [Required]
        [StringLength(20)]
        public string DaysOfWeek { get; set; } = string.Empty; // e.g. "2,4,6"

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active"; // Active / Closed

        public int? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }

        [ForeignKey("LecturerId")]
        public virtual User? Lecturer { get; set; }

        [ForeignKey("FacilityId")]
        public virtual Facility? Facility { get; set; }

        [ForeignKey("RoomId")]
        public virtual Room? Room { get; set; }

        [ForeignKey("ShiftId")]
        public virtual Shift? Shift { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }

        public virtual ICollection<ClassSession> ClassSessions { get; set; } = new List<ClassSession>();
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<Tuition> Tuitions { get; set; } = new List<Tuition>();
        public virtual ICollection<RoomChangeRequest> RoomChangeRequests { get; set; } = new List<RoomChangeRequest>();
    }
}

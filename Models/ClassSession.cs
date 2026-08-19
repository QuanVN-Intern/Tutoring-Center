using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduCenterManagement.Models
{
    [Table("ClassSessions")]
    public class ClassSession
    {
        [Key]
        public int SessionId { get; set; }

        public int ClassId { get; set; }

        public DateTime SessionDate { get; set; }

        public int RoomId { get; set; }

        public int ShiftId { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Scheduled"; // Scheduled / Completed / Cancelled

        [ForeignKey("ClassId")]
        public virtual Class? Class { get; set; }

        [ForeignKey("RoomId")]
        public virtual Room? Room { get; set; }

        [ForeignKey("ShiftId")]
        public virtual Shift? Shift { get; set; }

        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public virtual ICollection<RoomChangeRequest> RoomChangeRequests { get; set; } = new List<RoomChangeRequest>();
    }
}

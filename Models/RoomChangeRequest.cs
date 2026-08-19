using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduCenterManagement.Models
{
    [Table("RoomChangeRequests")]
    public class RoomChangeRequest
    {
        [Key]
        public int RequestId { get; set; }

        public int LecturerId { get; set; }

        public int ClassId { get; set; }

        public int? SessionId { get; set; }

        public int CurrentRoomId { get; set; }

        public int? RequestedRoomId { get; set; }

        public int CurrentShiftId { get; set; }

        public int? RequestedShiftId { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending"; // Pending / Approved / Rejected

        public int? ReviewedBy { get; set; }

        public DateTime? ReviewedAt { get; set; }

        [StringLength(255)]
        public string? ReviewNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("LecturerId")]
        public virtual User? Lecturer { get; set; }

        [ForeignKey("ClassId")]
        public virtual Class? Class { get; set; }

        [ForeignKey("SessionId")]
        public virtual ClassSession? Session { get; set; }

        [ForeignKey("CurrentRoomId")]
        public virtual Room? CurrentRoom { get; set; }

        [ForeignKey("RequestedRoomId")]
        public virtual Room? RequestedRoom { get; set; }

        [ForeignKey("CurrentShiftId")]
        public virtual Shift? CurrentShift { get; set; }

        [ForeignKey("RequestedShiftId")]
        public virtual Shift? RequestedShift { get; set; }

        [ForeignKey("ReviewedBy")]
        public virtual User? Reviewer { get; set; }
    }
}

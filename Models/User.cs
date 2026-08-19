using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduCenterManagement.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        public int RoleId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("RoleId")]
        public virtual Role? Role { get; set; }

        public virtual ICollection<Class> LecturerClasses { get; set; } = new List<Class>();
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<Attendance> RecordedAttendances { get; set; } = new List<Attendance>();
        public virtual ICollection<Attendance> StudentAttendances { get; set; } = new List<Attendance>();
        public virtual ICollection<Tuition> Tuitions { get; set; } = new List<Tuition>();
        public virtual ICollection<Payment> ReceivedPayments { get; set; } = new List<Payment>();
        public virtual ICollection<RoomChangeRequest> LecturerRequests { get; set; } = new List<RoomChangeRequest>();
        public virtual ICollection<RoomChangeRequest> ReviewedRequests { get; set; } = new List<RoomChangeRequest>();
    }
}

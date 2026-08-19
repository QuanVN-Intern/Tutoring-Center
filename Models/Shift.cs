using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduCenterManagement.Models
{
    [Table("Shifts")]
    public class Shift
    {
        [Key]
        public int ShiftId { get; set; }

        [Required]
        [StringLength(50)]
        public string ShiftName { get; set; } = string.Empty;

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
        public virtual ICollection<ClassSession> ClassSessions { get; set; } = new List<ClassSession>();
    }
}

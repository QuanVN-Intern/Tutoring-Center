using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduCenterManagement.Models
{
    [Table("Rooms")]
    public class Room
    {
        [Key]
        public int RoomId { get; set; }

        public int FacilityId { get; set; }

        [Required]
        [StringLength(20)]
        public string RoomNumber { get; set; } = string.Empty;

        public int? Capacity { get; set; } = 30;

        [Required]
        [StringLength(20)]
        public string Condition { get; set; } = "Open"; // Open / Closed

        [ForeignKey("FacilityId")]
        public virtual Facility? Facility { get; set; }

        public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
        public virtual ICollection<ClassSession> ClassSessions { get; set; } = new List<ClassSession>();
    }
}

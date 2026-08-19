using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduCenterManagement.Models
{
    [Table("Facilities")]
    public class Facility
    {
        [Key]
        public int FacilityId { get; set; }

        [Required]
        [StringLength(100)]
        public string FacilityName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Address { get; set; }

        public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
        public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
    }
}

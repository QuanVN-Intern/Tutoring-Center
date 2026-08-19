using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduCenterManagement.Models
{
    [Table("Enrollments")]
    public class Enrollment
    {
        [Key]
        public int EnrollmentId { get; set; }

        public int StudentId { get; set; }

        public int ClassId { get; set; }

        public DateTime EnrollDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Active"; // Active / Dropped

        [ForeignKey("StudentId")]
        public virtual User? Student { get; set; }

        [ForeignKey("ClassId")]
        public virtual Class? Class { get; set; }
    }
}

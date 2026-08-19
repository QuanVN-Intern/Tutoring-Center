using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduCenterManagement.Models
{
    [Table("Subjects")]
    public class Subject
    {
        [Key]
        public int SubjectId { get; set; }

        [Required]
        [StringLength(50)]
        public string SubjectName { get; set; } = string.Empty;

        public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
    }
}

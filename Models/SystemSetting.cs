using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduCenterManagement.Models
{
    [Table("SystemSettings")]
    public class SystemSetting
    {
        [Key]
        [StringLength(50)]
        public string SettingKey { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string SettingValue { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }
    }
}

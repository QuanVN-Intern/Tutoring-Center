using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduCenterManagement.Models
{
    [Table("Tuitions")]
    public class Tuition
    {
        [Key]
        public int TuitionId { get; set; }

        public int StudentId { get; set; }

        public int ClassId { get; set; }

        public DateTime PeriodFrom { get; set; }

        public DateTime PeriodTo { get; set; }

        public int SessionsAttended { get; set; } = 0;

        [Column(TypeName = "decimal(12,0)")]
        public decimal AmountPerSession { get; set; } = 200000;

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column(TypeName = "decimal(12,0)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(20)]
        public string PaymentStatus { get; set; } = "Unpaid"; // Paid / Unpaid

        public DateTime? DueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("StudentId")]
        public virtual User? Student { get; set; }

        [ForeignKey("ClassId")]
        public virtual Class? Class { get; set; }

        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}

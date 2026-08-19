using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduCenterManagement.Models
{
    [Table("Payments")]
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        public int TuitionId { get; set; }

        [Column(TypeName = "decimal(12,0)")]
        public decimal AmountPaid { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? PaymentMethod { get; set; } // Cash, Bank Transfer, ...

        public int? ReceivedBy { get; set; }

        [StringLength(50)]
        public string? ReceiptNumber { get; set; }

        [ForeignKey("TuitionId")]
        public virtual Tuition? Tuition { get; set; }

        [ForeignKey("ReceivedBy")]
        public virtual User? Receiver { get; set; }
    }
}

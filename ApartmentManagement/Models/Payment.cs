using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApartmentManagement.Models
{
    public enum PaymentType
    {
        Rent,
        Gas,
        Venue,
        Others
    }

    public enum PaymentStatus
    {
        Unpaid,
        Pending,
        Paid
    }

    public class Payment
    {
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [ForeignKey("TenantId")]
        public Tenant? Tenant { get; set; }

        [Required]
        [Display(Name = "Bill Date")]
        [DataType(DataType.Date)]
        public DateTime BillDate { get; set; }

        [Required]
        [Display(Name = "Month")]
        [DataType(DataType.Date)]
        public DateTime Month { get; set; }

        [Required]
        public PaymentType Type { get; set; }

        [Required]
        [Display(Name = "Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Display(Name = "Challan Document")]
        public string? ChallanDocumentPath { get; set; }

        [Display(Name = "Payment Receipt")]
        public string? PaymentReceiptPath { get; set; }

        [Display(Name = "Transaction ID")]
        public string? TransactionId { get; set; }

        [Display(Name = "Payment Method")]
        public string? PaymentMethod { get; set; }
        [Required]
        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Unpaid;

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? PaidAt { get; set; }
    }
}


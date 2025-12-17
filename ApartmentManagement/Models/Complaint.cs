using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApartmentManagement.Models
{
    public enum ComplaintStatus
    {
        Pending,
        InProgress,
        Resolved
    }

    public class Complaint
    {
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [ForeignKey("TenantId")]
        public Tenant? Tenant { get; set; }

        [Required]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime ComplaintDate { get; set; }

        [Display(Name = "Image")]
        public string? ImagePath { get; set; }

        public ComplaintStatus Status { get; set; } = ComplaintStatus.Pending;

        [Display(Name = "Admin Response")]
        public string? AdminResponse { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ResolvedAt { get; set; }
    }
}


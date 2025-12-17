using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApartmentManagement.Models
{
    public enum VisitStatus
    {
        Pending,
        Approved,
        Rejected,
        Rescheduled,
        Visited,
        Completed
    }

    public class VisitRequest
    {
        public int Id { get; set; }

        [Required]
        public int ApartmentId { get; set; }

        [ForeignKey("ApartmentId")]
        public Apartment? Apartment { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [Required]
        [Display(Name = "Requested Date")]
        [DataType(DataType.Date)]
        public DateTime RequestedDate { get; set; }

        [Required]
        [Display(Name = "Requested Time")]
        [DataType(DataType.Time)]
        public TimeSpan RequestedTime { get; set; }

        [Display(Name = "Suggested Date")]
        [DataType(DataType.Date)]
        public DateTime? SuggestedDate { get; set; }

        [Display(Name = "Suggested Time")]
        [DataType(DataType.Time)]
        public TimeSpan? SuggestedTime { get; set; }

        public VisitStatus Status { get; set; } = VisitStatus.Pending;

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}


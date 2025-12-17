using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApartmentManagement.Models
{
    public enum VenueType
    {
        Garden,
        Pool,
        Hall,
        Rooftop
    }

    public enum VenueBookingStatus
    {
        Pending,
        Approved,
        Rejected,
        Completed
    }

    public class VenueBooking
    {
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [ForeignKey("TenantId")]
        public Tenant? Tenant { get; set; }

        [Required]
        public VenueType VenueType { get; set; }

        [Required]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime BookingDate { get; set; }

        [Required]
        [Display(Name = "Time")]
        [DataType(DataType.Time)]
        public TimeSpan BookingTime { get; set; }

        [Display(Name = "End Time")]
        [DataType(DataType.Time)]
        public TimeSpan? EndTime { get; set; }

        [Required]
        [Display(Name = "Purpose")]
        public string Purpose { get; set; } = string.Empty;

        [Display(Name = "Number of Guests")]
        public int? NumberOfGuests { get; set; }

        public VenueBookingStatus Status { get; set; } = VenueBookingStatus.Pending;

        [Display(Name = "Admin Notes")]
        public string? AdminNotes { get; set; }

        [Display(Name = "Cleaning Scheduled")]
        public bool CleaningScheduled { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}


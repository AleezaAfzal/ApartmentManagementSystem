using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApartmentManagement.Models
{
    public class Tenant
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [Required]
        public int ApartmentId { get; set; }

        [ForeignKey("ApartmentId")]
        public Apartment? Apartment { get; set; }

        [Required]
        [Display(Name = "CNIC/ID Number")]
        public string CNIC { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Permanent Address")]
        public string PermanentAddress { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Rent Plan (months)")]
        public int RentPlanMonths { get; set; }

        [Required]
        [Display(Name = "Security Deposit")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SecurityDeposit { get; set; }

        [Required]
        [Display(Name = "Contract Start Date")]
        [DataType(DataType.Date)]
        public DateTime ContractStartDate { get; set; }

        [Required]
        [Display(Name = "Contract End Date")]
        [DataType(DataType.Date)]
        public DateTime ContractEndDate { get; set; }

        [Display(Name = "Monthly Rent")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyRent { get; set; }

        [Display(Name = "Agreement Document")]
        public string? AgreementDocumentPath { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
        public ICollection<VenueBooking> VenueBookings { get; set; } = new List<VenueBooking>();
    }
}


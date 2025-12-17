using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApartmentManagement.Models
{
    public enum ApartmentType
    {
        [Display(Name = "1BHK")]
        _1BHK,

        [Display(Name = "2BHK")]
        _2BHK,

        [Display(Name = "3BHK")]
        _3BHK,
        Office,
        CreativeStudio,
        Studio
    }

    public enum ApartmentStatus
    {
        Available,
        Rented,
        Maintenance
    }

    public class Apartment
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Apartment Number/Name")]
        public string ApartmentNumber { get; set; } = string.Empty;

        [Required]
        public ApartmentType Type { get; set; }

        [Required]
        [Display(Name = "Floor Number")]
        public int FloorNumber { get; set; }

        [Required]
        [Display(Name = "Size (sq ft)")]
        public decimal Size { get; set; }

        [Required]
        [Display(Name = "Base Rent Rate (per month)")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseRentRate { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        public string? Photos { get; set; } 

        public ApartmentStatus Status { get; set; } = ApartmentStatus.Available;

        [Required]
        public int BuildingId { get; set; }

        [ForeignKey("BuildingId")]
        public Building? Building { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<VisitRequest> VisitRequests { get; set; } = new List<VisitRequest>();
        public ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}


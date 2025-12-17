using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApartmentManagement.Models
{
    public class Building
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Building Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Full Address")]
        public string FullAddress { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Number of Floors")]
        public int NumberOfFloors { get; set; }

        [Required]
        public string OwnerId { get; set; } = string.Empty;

        [ForeignKey("OwnerId")]
        public ApplicationUser? Owner { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Apartment> Apartments { get; set; } = new List<Apartment>();
    }
}


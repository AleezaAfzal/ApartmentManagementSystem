
using ApartmentManagement.Models;
using System.ComponentModel.DataAnnotations;
namespace ApartmentManagement.ViewModels
{
    public class AddApartmentViewModel
    {
        public int BuildingId { get; set; }

        [Required(ErrorMessage = "Apartment number is required")]
        [StringLength(50)]
        [Display(Name = "Apartment Number")]
        public string? ApartmentNumber { get; set; }

        [Required(ErrorMessage = "Apartment type is required")]
        [Display(Name = "Apartment Type")]
        public ApartmentType? Type { get; set; }

        [Required(ErrorMessage = "Floor number is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Floor number must be positive")]
        [Display(Name = "Floor Number")]
        public int? FloorNumber { get; set; }

        [Required(ErrorMessage = "Size is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Size must be greater than 0")]
        [Display(Name = "Size (sq ft)")]
        public decimal? Size { get; set; }

        [Required(ErrorMessage = "Base rent rate is required")]
        [Range(1000, double.MaxValue, ErrorMessage = "Rent must be greater than 0")]
        [Display(Name = "Base Rent Rate ($)")]
        public decimal? BaseRentRate { get; set; }

        [StringLength(1000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }
        public int Id { get; set; }
        public List<string>? ExistingPhotos { get; set; }
        public List<IFormFile>? NewPhotos { get; set; }
    }
}
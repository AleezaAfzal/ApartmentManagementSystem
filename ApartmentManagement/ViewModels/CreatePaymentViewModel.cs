using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace ApartmentManagement.ViewModels
{
    public class CreatePaymentViewModel
    {
        [Required]
        [Display(Name = "Apartment")]
        public int ApartmentId { get; set; }

        // Hidden field to store the actual TenantId found via JS/Controller logic
        [Required]
        public int TenantId { get; set; }

        [Display(Name = "Tenant Name")]
        public string? TenantName { get; set; } // Read-only for display

        [Required]
        [DataType(DataType.Date)]
        public DateTime BillDate { get; set; } = DateTime.Today;

        [Required]
        [DataType(DataType.Date)]
        public DateTime Month { get; set; } = DateTime.Today;

        [Required]
        public ApartmentManagement.Models.PaymentType Type { get; set; }
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(10);

        [Required]
        public decimal Amount { get; set; }

        [Display(Name = "Upload Challan (PDF/Image)")]
        public IFormFile? ChallanFile { get; set; }
    }
}
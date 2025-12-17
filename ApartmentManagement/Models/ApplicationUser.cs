using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ApartmentManagement.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        public override string? PhoneNumber { get; set; }

        [Display(Name = "CNIC/ID Number")]
        public string? CNIC { get; set; }

        [Display(Name = "CNIC/ID Document")]
        public string? CNICDocumentPath { get; set; }

        [Display(Name = "Permanent Address")]
        public string? PermanentAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}


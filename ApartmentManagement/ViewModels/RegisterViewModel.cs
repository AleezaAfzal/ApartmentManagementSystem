using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ApartmentManagement.ViewModels
{
    public class RegisterViewModel : IValidatableObject
    {
        [Required]
        [Display(Name = "Full Name")]
        [StringLength(20, ErrorMessage = "{0} cannot be longer than {1} characters.")]
        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Full name can contain only letters and spaces.")]
        public string FullName { get; set; } = string.Empty;


        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Phone Number")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "Phone number must be exactly 11 digits.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "CNIC/ID Number")]
        [RegularExpression(@"^\d{5}-\d{7}-\d{1}$", ErrorMessage = "CNIC must be in format 12345-1234567-1")]
        public string? CNIC { get; set; }

        [Display(Name = "Upload CNIC/ID Document")]
        public IFormFile? CNICDocument { get; set; }

        [Display(Name = "Building Name")]
        [StringLength(20, ErrorMessage = "{0} cannot be longer than {1} characters.")]
        public string? BuildingName { get; set; }

        [Display(Name = "Building Address")]
        [StringLength(60, ErrorMessage = "{0} cannot be longer than {1} characters.")]
        public string? BuildingAddress { get; set; }

        [Display(Name = "Register as Owner/Admin")]
        public bool IsOwner { get; set; } = false;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // If registering as Owner, CNIC, CNICDocument, BuildingName and BuildingAddress are required
            if (IsOwner)
            {
                if (string.IsNullOrWhiteSpace(CNIC))
                {
                    yield return new ValidationResult("CNIC is required when registering as Owner.", new[] { nameof(CNIC) });
                }

                if (CNICDocument == null)
                {
                    yield return new ValidationResult("CNIC document upload is required when registering as Owner.", new[] { nameof(CNICDocument) });
                }

                if (string.IsNullOrWhiteSpace(BuildingName))
                {
                    yield return new ValidationResult("Building name is required when registering as Owner.", new[] { nameof(BuildingName) });
                }

                if (string.IsNullOrWhiteSpace(BuildingAddress))
                {
                    yield return new ValidationResult("Building address is required when registering as Owner.", new[] { nameof(BuildingAddress) });
                }
            }

            // If BuildingName provided (non-owner scenario), ensure BuildingAddress present (optional business rule)
            if (!IsOwner && !string.IsNullOrWhiteSpace(BuildingName) && string.IsNullOrWhiteSpace(BuildingAddress))
            {
                yield return new ValidationResult("Building address is required when building name is provided.", new[] { nameof(BuildingAddress) });
            }
        }
    }
}
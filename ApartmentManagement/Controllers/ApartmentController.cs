using ApartmentManagement.Data;
using ApartmentManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagement.Controllers
{
    public class ApartmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ApartmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public async Task<IActionResult> Category(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return RedirectToAction("Index", "Home");
            }

           
            if (!Enum.TryParse<ApartmentType>(type, out var apartmentType))
            {
                return NotFound();
            }

            var apartments = await _context.Apartments
                .Include(a => a.Building)
                .Include(a => a.Reviews)
                .Where(a => a.Type == apartmentType && a.Status == ApartmentStatus.Available)
                .OrderBy(a => a.BaseRentRate)
                .ToListAsync();

            ViewData["CategoryType"] = type;
            ViewData["Title"] = $"{type} Apartments";

            
            string viewName = type switch
            {
                "Family" => "FamilyApartments",
                "Studio" => "StudioApartments",
                "Office" => "OfficeApartments",
                "CreativeStudio" => "CreativeStudioApartments",
                
                _ => "FamilyApartments"
            };

            return View($"~/Views/Categories/{viewName}.cshtml", apartments);
        }

      
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApartmentManagement.Data;
using ApartmentManagement.Models;

namespace ApartmentManagement.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [AllowAnonymous]
        public async Task<IActionResult> BrowseApartments(string? type, decimal? minRent, decimal? maxRent, decimal? minSize, decimal? maxSize)
        {
            var query = _context.Apartments
                .Include(a => a.Building)
                .Include(a => a.Reviews)
                .Where(a => a.Status == ApartmentStatus.Available)
                .AsQueryable();

            if (!string.IsNullOrEmpty(type) && Enum.TryParse<ApartmentType>(type, out var apartmentType))
            {
                query = query.Where(a => a.Type == apartmentType);
            }

            if (minRent.HasValue)
            {
                query = query.Where(a => a.BaseRentRate >= minRent.Value);
            }

            if (maxRent.HasValue)
            {
                query = query.Where(a => a.BaseRentRate <= maxRent.Value);
            }

            if (minSize.HasValue)
            {
                query = query.Where(a => a.Size >= minSize.Value);
            }

            if (maxSize.HasValue)
            {
                query = query.Where(a => a.Size <= maxSize.Value);
            }

            var apartments = await query.ToListAsync();

            ViewBag.Types = Enum.GetValues(typeof(ApartmentType)).Cast<ApartmentType>().ToList();

            return View(apartments);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ApartmentDetails(int id)
        {
            var apartment = await _context.Apartments
                .Include(a => a.Building)
                .Include(a => a.Reviews)
                .ThenInclude(r => r.Tenant)
                .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (apartment == null) return NotFound();

           
            if (apartment.Reviews.Any())
            {
                ViewBag.AverageRating = apartment.Reviews.Average(r => r.Rating);
            }

            return View(apartment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookVisit(int apartmentId, DateTime requestedDate, TimeSpan requestedTime)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Check if user is already a tenant
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Contains("Tenant"))
            {
                TempData["Error"] = "You are already a tenant. Please use the tenant dashboard.";
                return RedirectToAction(nameof(ApartmentDetails), new { id = apartmentId });
            }

            var visitRequest = new VisitRequest
            {
                ApartmentId = apartmentId,
                UserId = user.Id,
                RequestedDate = requestedDate,
                RequestedTime = requestedTime,
                Status = VisitStatus.Pending,
                CreatedAt = DateTime.Now
            };

            _context.VisitRequests.Add(visitRequest);
            await _context.SaveChangesAsync();

            // TODO: Send notification email to owner

            TempData["Success"] = "Visit request submitted successfully. The owner will review it shortly.";
            return RedirectToAction(nameof(ApartmentDetails), new { id = apartmentId });
        }

        [HttpGet]
        public async Task<IActionResult> MyVisits()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var visits = await _context.VisitRequests
                .Include(v => v.Apartment)
                .ThenInclude(a => a.Building)
                .Where(v => v.UserId == user.Id)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            return View(visits);
        }

    }
}


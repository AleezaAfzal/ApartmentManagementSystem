using Microsoft.AspNetCore.Mvc;
using ApartmentManagement.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace ApartmentManagement.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["PageType"] = "Home";
            ViewData["IsHome"] = true;

            var apartments = await _context.Apartments
                .Include(a => a.Building)
                .Where(a => a.Status == Models.ApartmentStatus.Available)
                .ToListAsync();

            return View(apartments);
        }

        // ==========================================
        // ADD THIS NEW METHOD FOR THE PUBLIC REVIEWS PAGE
        // ==========================================
        public async Task<IActionResult> Reviews()
        {
            var reviews = await _context.Reviews
                .Include(r => r.Tenant)
                    .ThenInclude(t => t.User) // Get User to show Name (e.g. "Simran Butt")
                .Include(r => r.Apartment)
                    .ThenInclude(a => a.Building) // Get Building Name
                .OrderByDescending(r => r.CreatedAt) // Show newest first
                .ToListAsync();

            return View(reviews);
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApartmentManagement.Data;
using ApartmentManagement.Models;
using ApartmentManagement.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

namespace ApartmentManagement.Controllers
{
    [Authorize(Roles = "Owner")]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PaymentGenerationService _paymentService;
        private readonly IWebHostEnvironment _hostEnvironment;

        public PaymentController(
            ApplicationDbContext context,
            PaymentGenerationService paymentService,
            IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _paymentService = paymentService;
            _hostEnvironment = hostEnvironment;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateMonthlyPayments()
        {
            await _paymentService.GenerateMonthlyPayments();
            TempData["Success"] = "Monthly payments generated successfully.";
            return RedirectToAction("Payments", "Owner");
        }

        [HttpGet]
        public async Task<IActionResult> AddExtraBill(int tenantId)
        {
            var tenant = await _context.Tenants
                .Include(t => t.Apartment)
                .FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant == null) return NotFound();

            ViewBag.Tenant = tenant;
            ViewBag.PaymentTypes = Enum.GetValues(typeof(PaymentType)).Cast<PaymentType>().ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExtraBill(int tenantId, PaymentType type, decimal amount, DateTime month, IFormFile? challan)
        {
            string? challanPath = null;

            if (challan != null)
            {
                var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "challans");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + challan.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await challan.CopyToAsync(fileStream);
                }
                challanPath = "/uploads/challans/" + uniqueFileName;
            }

            await _paymentService.GenerateExtraBill(tenantId, type, amount, month, challanPath);

            TempData["Success"] = "Extra bill added successfully.";
            return RedirectToAction("Payments", "Owner");
        }
    }
}


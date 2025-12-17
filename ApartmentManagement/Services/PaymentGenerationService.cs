using ApartmentManagement.Data;
using ApartmentManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagement.Services
{
    public class PaymentGenerationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentGenerationService> _logger;

        public PaymentGenerationService(ApplicationDbContext context, ILogger<PaymentGenerationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task GenerateMonthlyPayments()
        {
            var tenants = await _context.Tenants
                .Include(t => t.Apartment)
                .Where(t => t.ContractEndDate >= DateTime.Today)
                .ToListAsync();

            var currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

            foreach (var tenant in tenants)
            {
                // Check if payment already exists for this month
                var existingPayment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.TenantId == tenant.Id
                        && p.Type == PaymentType.Rent
                        && p.Month.Year == currentMonth.Year
                        && p.Month.Month == currentMonth.Month);

                if (existingPayment == null)
                {
                    // Generate rent payment
                    var rentPayment = new Payment
                    {
                        TenantId = tenant.Id,
                        BillDate = DateTime.Today,
                        Month = currentMonth,
                        Type = PaymentType.Rent,
                        Amount = tenant.MonthlyRent,
                        Status = PaymentStatus.Unpaid,
                        CreatedAt = DateTime.Now
                    };

                    _context.Payments.Add(rentPayment);
                }

                // Generate other monthly bills (Gas, etc.) if needed
                // This can be customized based on requirements
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Monthly payments generated successfully.");
        }

        public async Task GenerateExtraBill(int tenantId, PaymentType type, decimal amount, DateTime month, string? challanPath = null)
        {
            var payment = new Payment
            {
                TenantId = tenantId,
                BillDate = DateTime.Today,
                Month = month,
                Type = type,
                Amount = amount,
                ChallanDocumentPath = challanPath,
                Status = PaymentStatus.Unpaid,
                CreatedAt = DateTime.Now
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
        }
    }
}


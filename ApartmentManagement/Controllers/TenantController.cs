using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApartmentManagement.Data;
using ApartmentManagement.Models;
using Microsoft.AspNetCore.Hosting;

namespace ApartmentManagement.Controllers
{
    [Authorize(Roles = "Tenant")]
    public class TenantController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _hostEnvironment;

        public TenantController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var tenant = await _context.Tenants
                .Include(t => t.Apartment!)
                    .ThenInclude(a => a.Building)
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (tenant == null) return NotFound();

            var payments = await _context.Payments
                .Where(p => p.TenantId == tenant.Id)
                .OrderByDescending(p => p.BillDate)
                .Take(5)
                .ToListAsync();

            var recentComplaints = await _context.Complaints
                .Where(c => c.TenantId == tenant.Id)
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToListAsync();

            var upcomingVenueBookings = await _context.VenueBookings
                .Where(v => v.TenantId == tenant.Id && v.BookingDate >= DateTime.Today)
                .OrderBy(v => v.BookingDate)
                .ThenBy(v => v.BookingTime)
                .Take(5)
                .ToListAsync();

            ViewBag.Tenant = tenant;
            ViewBag.Payments = payments;
            ViewBag.RecentComplaints = recentComplaints;
            ViewBag.UpcomingVenueBookings = upcomingVenueBookings;

            return View();
        }

        public async Task<IActionResult> Payments(string? searchStatus, DateTime? searchMonth, PaymentType? searchType)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return NotFound();

            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.UserId == userId);
            if (tenant == null) return NotFound();

            var query = _context.Payments
                .Where(p => p.TenantId == tenant.Id)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchStatus) && Enum.TryParse(searchStatus, out PaymentStatus status))
            {
                query = query.Where(p => p.Status == status);
            }
            if (searchMonth.HasValue)
            {
                query = query.Where(p => p.Month.Month == searchMonth.Value.Month && p.Month.Year == searchMonth.Value.Year);
            }
            if (searchType.HasValue)
            {
                query = query.Where(p => p.Type == searchType.Value);
            }

            var payments = await query.OrderByDescending(p => p.BillDate).ToListAsync();

            ViewData["CurrentStatus"] = searchStatus;
            ViewData["CurrentMonth"] = searchMonth?.ToString("yyyy-MM");
            ViewData["CurrentType"] = searchType;

            return View(payments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitPayment(int paymentId, string paymentMethod, string transactionId, IFormFile paymentReceipt)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (tenant == null) return NotFound();

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.TenantId == tenant.Id);

            if (payment == null)
            {
                TempData["Error"] = "Payment record not found.";
                return RedirectToAction(nameof(Payments));
            }

            if (payment.Status != PaymentStatus.Unpaid)
            {
                TempData["Error"] = "This bill is already processing or paid.";
                return RedirectToAction(nameof(Payments));
            }

            if (paymentReceipt != null && paymentReceipt.Length > 0)
            {
                var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath ?? string.Empty, "uploads", "receipts");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + paymentReceipt.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await paymentReceipt.CopyToAsync(fileStream);
                }
                payment.PaymentReceiptPath = "/uploads/receipts/" + uniqueFileName;
            }

            payment.PaymentMethod = paymentMethod;
            payment.TransactionId = transactionId;
            payment.Status = PaymentStatus.Pending;

            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment proof submitted successfully! Waiting for verification.";
            return RedirectToAction(nameof(Payments));
        }

        [HttpGet]
        public async Task<IActionResult> Complaints()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (tenant == null) return NotFound();

            var complaints = await _context.Complaints
                .Where(c => c.TenantId == tenant.Id)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(complaints);
        }

        [HttpGet]
        public async Task<IActionResult> SubmitComplaint(int? id)
        {
            if (id.HasValue)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return NotFound();

                var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.UserId == user.Id);
                if (tenant == null) return NotFound();

                var complaint = await _context.Complaints
                    .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenant.Id);

                if (complaint == null) return NotFound();
                return View(complaint);
            }

            return View(new Complaint { ComplaintDate = DateTime.Now });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitComplaint(Complaint complaint, IFormFile? image)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (tenant == null) return NotFound();

            if (ModelState.IsValid)
            {
                if (image != null)
                {
                    complaint.ImagePath = await UploadFile(image, "complaints");
                }

                if (complaint.Id > 0)
                {
                    var existingComplaint = await _context.Complaints
                        .FirstOrDefaultAsync(c => c.Id == complaint.Id && c.TenantId == tenant.Id);

                    if (existingComplaint == null) return NotFound();

                    existingComplaint.Title = complaint.Title;
                    existingComplaint.Description = complaint.Description;
                    existingComplaint.ComplaintDate = complaint.ComplaintDate;

                    if (image != null)
                    {
                        existingComplaint.ImagePath = complaint.ImagePath;
                    }

                    _context.Update(existingComplaint);
                    TempData["Success"] = "Complaint updated successfully.";
                }
                else
                {
                    complaint.TenantId = tenant.Id;
                    complaint.Status = ComplaintStatus.Pending;
                    complaint.CreatedAt = DateTime.Now;
                    _context.Complaints.Add(complaint);
                    TempData["Success"] = "Complaint submitted successfully.";
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Complaints));
            }

            return View(complaint);
        }

        [HttpGet]
        public IActionResult Edit(int? id)
        {
            return RedirectToAction(nameof(SubmitComplaint), new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (tenant == null) return NotFound();

            var complaint = await _context.Complaints
                .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenant.Id);

            if (complaint != null)
            {
                _context.Complaints.Remove(complaint);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Complaint deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Complaint not found or permission denied.";
            }

            return RedirectToAction(nameof(Complaints));
        }

        [HttpGet]
        public async Task<IActionResult> VenueBookings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (tenant == null) return NotFound();

            var bookings = await _context.VenueBookings
                .Where(v => v.TenantId == tenant.Id)
                .OrderByDescending(v => v.BookingDate)
                .ThenByDescending(v => v.BookingTime)
                .ToListAsync();

            return View(bookings);
        }

        [HttpGet]
        public IActionResult BookVenue()
        {
            ViewBag.VenueTypes = Enum.GetValues(typeof(VenueType)).Cast<VenueType>().ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookVenue(VenueBooking booking)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (tenant == null) return NotFound();

            if (ModelState.IsValid)
            {
                var conflictingBookings = await _context.VenueBookings
                    .Where(v => v.VenueType == booking.VenueType
                        && v.BookingDate == booking.BookingDate
                        && v.Status == VenueBookingStatus.Approved
                        && v.Id != booking.Id)
                    .ToListAsync();

                if (conflictingBookings.Any())
                {
                    var conflict = conflictingBookings.FirstOrDefault(v =>
                        (v.BookingTime <= booking.BookingTime && (v.EndTime ?? v.BookingTime.Add(TimeSpan.FromHours(2))) > booking.BookingTime) ||
                        (booking.BookingTime <= v.BookingTime && (booking.EndTime ?? booking.BookingTime.Add(TimeSpan.FromHours(2))) > v.BookingTime));

                    if (conflict != null)
                    {
                        ModelState.AddModelError("", "This time slot is already booked.");
                        ViewBag.VenueTypes = Enum.GetValues(typeof(VenueType)).Cast<VenueType>().ToList();
                        return View(booking);
                    }
                }

                booking.TenantId = tenant.Id;
                booking.Status = VenueBookingStatus.Pending;
                booking.CreatedAt = DateTime.Now;

                _context.VenueBookings.Add(booking);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Venue booking request submitted.";
                return RedirectToAction(nameof(VenueBookings));
            }

            ViewBag.VenueTypes = Enum.GetValues(typeof(VenueType)).Cast<VenueType>().ToList();
            return View(booking);
        }

        [HttpGet]
        public async Task<IActionResult> Reviews()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (tenant == null) return NotFound();

            var reviews = await _context.Reviews
                .Include(r => r.Apartment!)
                    .ThenInclude(a => a.Building)
                .Where(r => r.TenantId == tenant.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reviews);
        }

        [HttpGet]
        public async Task<IActionResult> SubmitReview(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var tenant = await _context.Tenants
                .Include(t => t.Apartment!)
                    .ThenInclude(a => a.Building)
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (tenant == null || tenant.Apartment == null)
            {
                TempData["Error"] = "You are not assigned to an apartment.";
                return RedirectToAction(nameof(Dashboard));
            }

            ViewBag.Apartment = tenant.Apartment;

            if (id.HasValue)
            {
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenant.Id);

                if (existingReview == null) return NotFound();
                return View(existingReview);
            }

            var previousReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.TenantId == tenant.Id && r.ApartmentId == tenant.ApartmentId);

            if (previousReview != null)
            {
                TempData["Info"] = "You already reviewed this apartment. You can edit it here.";
                return View(previousReview);
            }

            return View(new Review { ApartmentId = tenant.ApartmentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(Review review)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (tenant == null) return NotFound();

            if (ModelState.IsValid)
            {
                if (review.Id > 0)
                {
                    var existingReview = await _context.Reviews
                        .FirstOrDefaultAsync(r => r.Id == review.Id && r.TenantId == tenant.Id);

                    if (existingReview == null) return NotFound();

                    existingReview.Title = review.Title;
                    existingReview.Comment = review.Comment;
                    existingReview.Rating = review.Rating;

                    _context.Update(existingReview);
                    TempData["Success"] = "Review updated successfully.";
                }
                else
                {
                    review.TenantId = tenant.Id;
                    review.CreatedAt = DateTime.Now;
                    _context.Reviews.Add(review);
                    TempData["Success"] = "Review submitted successfully.";
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Reviews));
            }

            var apartment = await _context.Apartments
                .Include(a => a.Building)
                .FirstOrDefaultAsync(a => a.Id == review.ApartmentId);
            ViewBag.Apartment = apartment;

            return View(review);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (tenant == null) return NotFound();

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenant.Id);

            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Review deleted successfully.";
            }

            return RedirectToAction(nameof(Reviews));
        }

        private async Task<string> UploadFile(IFormFile file, string folderName)
        {
            var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath ?? string.Empty, "uploads", folderName);
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return "/uploads/" + folderName + "/" + uniqueFileName;
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var tenant = await _context.Tenants
                .Include(t => t.User)
                .Include(t => t.Apartment!)
                    .ThenInclude(a => a.Building)
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (tenant == null) return NotFound();

            return View(tenant);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(Tenant model, string FullName, string PhoneNumber, string? Email, string? CurrentPassword, string? NewPassword, string? ConfirmPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Update basic profile fields
            user.FullName = FullName ?? user.FullName;
            user.PhoneNumber = PhoneNumber ?? user.PhoneNumber;

            // If email changed, update email and username
            if (!string.IsNullOrEmpty(Email) && !string.Equals(Email, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, Email);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var err in setEmailResult.Errors) ModelState.AddModelError("Email", err.Description);
                }
                var setUserName = await _userManager.SetUserNameAsync(user, Email);
                if (!setUserName.Succeeded)
                {
                    foreach (var err in setUserName.Errors) ModelState.AddModelError("Email", err.Description);
                }
            }

            // Handle password change if requested
            if (!string.IsNullOrEmpty(CurrentPassword) || !string.IsNullOrEmpty(NewPassword) || !string.IsNullOrEmpty(ConfirmPassword))
            {
                if (string.IsNullOrEmpty(CurrentPassword)) ModelState.AddModelError("CurrentPassword", "Current password is required to change password.");
                if (string.IsNullOrEmpty(NewPassword)) ModelState.AddModelError("NewPassword", "New password is required.");
                if (NewPassword != ConfirmPassword) ModelState.AddModelError("ConfirmPassword", "New password and confirmation do not match.");

                if (ModelState.IsValid)
                {
                    var changeResult = await _userManager.ChangePasswordAsync(user, CurrentPassword!, NewPassword!);
                    if (!changeResult.Succeeded)
                    {
                        foreach (var err in changeResult.Errors) ModelState.AddModelError(string.Empty, err.Description);
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                // Re-fetch tenant to display
                var tenant = await _context.Tenants
                    .Include(t => t.User)
                    .Include(t => t.Apartment!)
                        .ThenInclude(a => a.Building)
                    .FirstOrDefaultAsync(t => t.UserId == user.Id);

                if (tenant == null) return NotFound();
                return View(tenant);
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var err in updateResult.Errors) ModelState.AddModelError(string.Empty, err.Description);

                var tenant = await _context.Tenants
                    .Include(t => t.User)
                    .Include(t => t.Apartment!)
                        .ThenInclude(a => a.Building)
                    .FirstOrDefaultAsync(t => t.UserId == user.Id);

                if (tenant == null) return NotFound();
                return View(tenant);
            }

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public async Task<IActionResult> CalendarEvents(DateTime? start, DateTime? end)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new List<object>());

            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (tenant == null) return Json(new List<object>());

            DateTime from = start ?? DateTime.Today.AddMonths(-1);
            DateTime to = end ?? DateTime.Today.AddMonths(3);

            var events = new List<object>();

            // Payments due for this tenant
            var payments = await _context.Payments
                .Where(p => p.TenantId == tenant.Id && p.DueDate >= from && p.DueDate <= to)
                .ToListAsync();

            foreach (var p in payments)
            {
                events.Add(new
                {
                    id = "payment-" + p.Id,
                    title = $"Payment Due: Rs. {p.Amount:N0}",
                    start = p.DueDate.ToString("o"),
                    allDay = true,
                    color = p.Status == ApartmentManagement.Models.PaymentStatus.Unpaid ? "#f97316" : "#10b981"
                });
            }

            // Visits for tenant's apartment
            var visits = await _context.VisitRequests
                .Where(v => v.UserId == user.Id && v.RequestedDate >= from && v.RequestedDate <= to)
                .ToListAsync();

            foreach (var v in visits)
            {
                DateTime startDt = v.RequestedDate.Date + v.RequestedTime;
                events.Add(new
                {
                    id = "visit-" + v.Id,
                    title = $"Visit: {v.RequestedTime}",
                    start = startDt.ToString("o"),
                    allDay = false,
                    color = "#3b82f6"
                });
            }

            return Json(events);
        }
    }
}
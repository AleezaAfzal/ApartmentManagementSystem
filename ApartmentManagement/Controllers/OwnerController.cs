using ApartmentManagement.Data;
using ApartmentManagement.Models;
using ApartmentManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ApartmentManagement.Controllers
{
    [Authorize(Roles = "Owner")]
    public class OwnerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _hostEnvironment;

        public OwnerController(
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

            var buildings = await _context.Buildings
                .Where(b => b.OwnerId == user.Id)
                .Include(b => b.Apartments)
                .ToListAsync();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> AddApartment()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var building = await _context.Buildings.FirstOrDefaultAsync(b => b.OwnerId == user.Id);
            if (building == null)
            {
                TempData["Error"] = "No building found. Please add a building first.";
                return RedirectToAction("Dashboard");
            }

            var viewModel = new AddApartmentViewModel
            {
                BuildingId = building.Id
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddApartment(AddApartmentViewModel viewModel, List<IFormFile> photos)
        {
            if (photos != null && photos.Count > 0)
            {
                photos = photos.Where(file =>
                    !file.FileName.EndsWith(".avif", StringComparison.OrdinalIgnoreCase) &&
                    !file.FileName.EndsWith(".heic", StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }
            // ==============================================================================

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ModelState.AddModelError("", "Unable to identify owner.");
                return View(viewModel);
            }

            var building = await _context.Buildings.FirstOrDefaultAsync(b => b.OwnerId == user.Id);
            if (building == null || viewModel.BuildingId != building.Id)
            {
                ModelState.AddModelError("", "Invalid building.");
                return View(viewModel);
            }

            if (photos == null || photos.Count < 1)
            {
                ModelState.AddModelError("photos", "At least 1 photo is required.");
                return View(viewModel);
            }

            foreach (var photo in photos)
            {
                if (photo == null || photo.Length == 0 || photo.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("photos", "Each photo must be less than 5MB.");
                    return View(viewModel);
                }
                if (string.IsNullOrEmpty(photo.ContentType) || !photo.ContentType.StartsWith("image/"))
                {
                    ModelState.AddModelError("photos", "Only image files are allowed.");
                    return View(viewModel);
                }
            }

            if (ModelState.IsValid)
            {
                var apartment = new Apartment
                {
                    BuildingId = viewModel.BuildingId,
                    ApartmentNumber = viewModel.ApartmentNumber!,
                    Type = viewModel.Type!.Value,
                    FloorNumber = viewModel.FloorNumber!.Value,
                    Size = viewModel.Size!.Value,
                    BaseRentRate = viewModel.BaseRentRate!.Value,
                    Description = viewModel.Description
                };

                _context.Apartments.Add(apartment);
                await _context.SaveChangesAsync();

                var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath ?? string.Empty, "uploads", "apartments", apartment.Id.ToString());
                Directory.CreateDirectory(uploadsFolder);
                var photoPaths = new List<string>();

                for (int i = 0; i < photos.Count; i++)
                {
                    var photo = photos[i];
                    if (photo != null)
                    {
                        var fileName = $"photo{i + 1}{Path.GetExtension(photo.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await photo.CopyToAsync(stream);
                        }
                        photoPaths.Add($"/uploads/apartments/{apartment.Id}/{fileName}");
                    }
                }

                apartment.Photos = JsonConvert.SerializeObject(photoPaths);
                _context.Apartments.Update(apartment);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Apartment added successfully!";
                return RedirectToAction("Dashboard");
            }
            if (photos == null || photos.Count < 1)
    {
        ModelState.AddModelError("photos", "At least 1 photo is required.");
        return View(viewModel);
    }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> VisitRequests()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var visits = await _context.VisitRequests
                .Include(v => v.Apartment)
                .ThenInclude(a => a.Building)
                .Include(v => v.User)
                .Where(v => v.Apartment.Building.OwnerId == user.Id)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            return View(visits);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveVisit(int id)
        {
            var visit = await _context.VisitRequests.FindAsync(id);
            if (visit == null) return NotFound();

            visit.Status = VisitStatus.Approved;
            visit.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(VisitRequests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuggestNewTime(int id, DateTime suggestedDate, TimeSpan suggestedTime)
        {
            var visit = await _context.VisitRequests.FindAsync(id);
            if (visit == null) return NotFound();

            visit.SuggestedDate = suggestedDate;
            visit.SuggestedTime = suggestedTime;
            visit.Status = VisitStatus.Rescheduled;
            visit.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(VisitRequests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsVisited(int id)
        {
            var visit = await _context.VisitRequests.FindAsync(id);
            if (visit == null) return NotFound();

            visit.Status = VisitStatus.Visited;
            visit.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Visit marked as completed successfully.";

            return RedirectToAction(nameof(VisitRequests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectVisit(int id, string? reason)
        {
            var visit = await _context.VisitRequests.FindAsync(id);
            if (visit == null) return NotFound();

            visit.Status = VisitStatus.Rejected;
            visit.Notes = reason ?? visit.Notes;
            visit.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Visit request rejected.";

            return RedirectToAction(nameof(VisitRequests));
        }

        // GET: Owner/ConvertToTenant
        [HttpGet]
        public async Task<IActionResult> ConvertToTenant(int visitRequestId)
        {
            // 1. Fetch Visit with SAFETY Includes
            var visit = await _context.VisitRequests
                .Include(v => v.User)
                .Include(v => v.Apartment)
                    .ThenInclude(a => a.Building)
                .FirstOrDefaultAsync(v => v.Id == visitRequestId);

            // 2. Safety Checks (Prevents NullReferenceException)
            if (visit == null) return NotFound();

            // Check if critical relationships exist
            if (visit.User == null) return BadRequest("Error: The User associated with this request no longer exists.");
            if (visit.Apartment == null) return BadRequest("Error: The Apartment associated with this request no longer exists.");
            if (visit.Apartment.Building == null) return BadRequest("Error: The Apartment is not assigned to a Building.");

            // 3. Fetch Owner's Apartments (For the dropdown)
            // We access visit.Apartment.Building.OwnerId safely because we checked for nulls above
            var apartments = await _context.Apartments
                .Include(a => a.Building)
                .Where(a => a.Building != null && a.Building.OwnerId == visit.Apartment.Building.OwnerId)
                .ToListAsync();

            // 4. Setup ViewBag
            ViewBag.VisitRequest = visit;
            ViewBag.Apartments = apartments;

            // 5. Pre-fill Model
            var model = new Tenant
            {
                UserId = visit.UserId,
                User = visit.User, // Pass User to View to prevent View crash
                ApartmentId = visit.ApartmentId,
                Apartment = visit.Apartment, // Pass Apartment to View
                ContractStartDate = DateTime.Today,
                ContractEndDate = DateTime.Today.AddYears(1),
                MonthlyRent = visit.Apartment.BaseRentRate,
                RentPlanMonths = 12
            };

            return View(model);
        }

        // POST: Owner/ConvertToTenant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConvertToTenant(int visitRequestId, Tenant tenant, IFormFile? agreementDocument)
        {
            // 1. Re-fetch Visit Request to verify context
            var visit = await _context.VisitRequests
                .Include(v => v.User)
                .Include(v => v.Apartment)
                    .ThenInclude(a => a.Building)
                .AsNoTracking() // Optimization
                .FirstOrDefaultAsync(v => v.Id == visitRequestId);

            if (visit == null) return NotFound();

            // 2. Calculate End Date automatically
            if (tenant.ContractStartDate != default)
            {
                tenant.ContractEndDate = tenant.ContractStartDate.AddMonths(tenant.RentPlanMonths);
            }

            // 3. Process Form
            if (ModelState.IsValid)
            {
                // Handle File Upload
                if (agreementDocument != null)
                {
                    var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "agreements");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + agreementDocument.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await agreementDocument.CopyToAsync(fileStream);
                    }
                    tenant.AgreementDocumentPath = "/uploads/agreements/" + uniqueFileName;
                }

                // Set missing fields
                tenant.UserId = visit.UserId;
                tenant.CreatedAt = DateTime.Now;

                // Update Apartment Status
                var apartment = await _context.Apartments.FindAsync(tenant.ApartmentId);
                if (apartment != null)
                {
                    apartment.Status = ApartmentStatus.Rented;
                    _context.Update(apartment);
                }

                // Save Tenant
                _context.Tenants.Add(tenant);

                // Update User Role
                var user = await _userManager.FindByIdAsync(visit.UserId);
                if (user != null)
                {
                    if (!await _userManager.IsInRoleAsync(user, "Tenant"))
                    {
                        await _userManager.AddToRoleAsync(user, "Tenant");
                    }
                }

                // Close Visit Request
                // We need to attach/update the visit status directly
                var visitToUpdate = await _context.VisitRequests.FindAsync(visitRequestId);
                if (visitToUpdate != null)
                {
                    visitToUpdate.Status = VisitStatus.Completed;
                    _context.Update(visitToUpdate);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Tenant Onboarded Successfully!";
                return RedirectToAction(nameof(Tenants));
            }

            // --- IF VALIDATION FAILS (Re-populate View Data) ---

            // Safety check for re-population
            if (visit.Apartment != null && visit.Apartment.Building != null)
            {
                var apartmentsList = await _context.Apartments
                    .Include(a => a.Building)
                    .Where(a => a.Building != null && a.Building.OwnerId == visit.Apartment.Building.OwnerId)
                    .ToListAsync();
                ViewBag.Apartments = apartmentsList;
            }
            else
            {
                ViewBag.Apartments = new List<Apartment>();
            }

            ViewBag.VisitRequest = visit;

            // Restore User/Apartment objects to Model so View doesn't crash on @Model.User.Name
            tenant.User = visit.User;
            tenant.Apartment = visit.Apartment;

            return View(tenant);
        }

        [HttpGet]
        public async Task<IActionResult> Tenants(string searchStatus, ApartmentManagement.Models.ApartmentType? searchType)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var query = _context.Tenants
                .Include(t => t.User)
                .Include(t => t.Apartment)
                    .ThenInclude(a => a.Building)
                .Where(t => t.Apartment != null &&
                            t.Apartment.Building != null &&
                            t.Apartment.Building.OwnerId == user.Id)
                .AsQueryable();

            if (searchType.HasValue)
                query = query.Where(t => t.Apartment.Type == searchType.Value);

            if (!string.IsNullOrEmpty(searchStatus))
            {
                var today = DateTime.Today;
                if (searchStatus == "Active") query = query.Where(t => t.ContractEndDate >= today);
                else if (searchStatus == "Expired") query = query.Where(t => t.ContractEndDate < today);
            }

            ViewData["CurrentStatus"] = searchStatus;
            ViewData["CurrentType"] = searchType;

            var tenants = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
            return View(tenants);
        }

        // --- PAYMENT MANAGEMENT (CORRECTED) ---

        [HttpGet]
        public async Task<IActionResult> Payments(int? searchApartmentId, DateTime? searchMonth, PaymentType? searchType, PaymentStatus? searchStatus)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Populate Dropdown
            var ownerApartments = await _context.Apartments
                .Include(a => a.Building)
                .Where(a => a.Building.OwnerId == user.Id)
                .Select(a => new {
                    a.Id,
                    Name = $"{a.Building.Name} - Unit {a.ApartmentNumber}"
                })
                .ToListAsync();

            ViewBag.SearchApartmentList = new SelectList(ownerApartments, "Id", "Name", searchApartmentId);

            // Base Query
            var query = _context.Payments
                .Include(p => p.Tenant)
                    .ThenInclude(t => t.User)
                .Include(p => p.Tenant)
                    .ThenInclude(t => t.Apartment)
                        .ThenInclude(a => a.Building)
                .Where(p => p.Tenant.Apartment.Building.OwnerId == user.Id)
                .AsQueryable();

            // Filters
            if (searchApartmentId.HasValue) query = query.Where(p => p.Tenant.ApartmentId == searchApartmentId.Value);
            if (searchMonth.HasValue) query = query.Where(p => p.Month.Month == searchMonth.Value.Month && p.Month.Year == searchMonth.Value.Year);
            if (searchType.HasValue) query = query.Where(p => p.Type == searchType.Value);
            if (searchStatus.HasValue) query = query.Where(p => p.Status == searchStatus.Value);

            var payments = await query.OrderByDescending(p => p.BillDate).ToListAsync();
            ViewData["CurrentMonth"] = searchMonth?.ToString("yyyy-MM");

            return View(payments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPayment(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null) return NotFound();

            payment.Status = PaymentStatus.Paid;
            payment.PaidAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Payments));
        }
        // POST: /Owner/UnverifyPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnverifyPayment(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null) return NotFound();

            // Logic: Reject the proof and reset to Unpaid
            payment.Status = PaymentStatus.Unpaid;

            // Optional: We keep the old receipt path so you can still see what they uploaded, 
            // or you can set it to null if you want them to upload fresh. 
            // payment.PaymentReceiptPath = null; 

            payment.PaidAt = null;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Payment marked as Unverified/Unpaid.";

            return RedirectToAction(nameof(Payments));
        }

        [HttpGet]
        public async Task<IActionResult> AddPayment()
        {
            var user = await _userManager.GetUserAsync(User);
            var rentedApartments = await _context.Apartments
                .Include(a => a.Building)
                .Where(a => a.Building.OwnerId == user.Id && a.Status == ApartmentStatus.Rented)
                .Select(a => new { a.Id, DisplayText = $"{a.Building.Name} - Unit {a.ApartmentNumber}" })
                .ToListAsync();

            ViewBag.Apartments = new SelectList(rentedApartments, "Id", "DisplayText");
            return View(new CreatePaymentViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPayment(CreatePaymentViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                string? challanPath = null;
                if (viewModel.ChallanFile != null)
                {
                    var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "payments");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + viewModel.ChallanFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await viewModel.ChallanFile.CopyToAsync(fileStream);
                    }
                    challanPath = "/uploads/payments/" + uniqueFileName;
                }

                var payment = new Payment
                {
                    TenantId = viewModel.TenantId,
                    BillDate = viewModel.BillDate,
                    DueDate = viewModel.DueDate,
                    Month = viewModel.Month,
                    Type = viewModel.Type,
                    Amount = viewModel.Amount,
                    Status = PaymentStatus.Unpaid,
                    ChallanDocumentPath = challanPath,
                    CreatedAt = DateTime.Now
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Bill generated successfully!";
                return RedirectToAction(nameof(Payments));
            }

            var user = await _userManager.GetUserAsync(User);
            var rentedApartments = await _context.Apartments
                .Include(a => a.Building)
                .Where(a => a.Building.OwnerId == user.Id && a.Status == ApartmentStatus.Rented)
                .Select(a => new { a.Id, DisplayText = $"{a.Building.Name} - Unit {a.ApartmentNumber}" })
                .ToListAsync();

            ViewBag.Apartments = new SelectList(rentedApartments, "Id", "DisplayText");
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetTenantByApartment(int apartmentId)
        {
            var tenant = await _context.Tenants
                .Include(t => t.User)
                .Where(t => t.ApartmentId == apartmentId && t.ContractEndDate >= DateTime.Today)
                .OrderByDescending(t => t.ContractEndDate)
                .FirstOrDefaultAsync();

            if (tenant == null) return Json(new { success = false, message = "No active tenant found." });

            return Json(new
            {
                success = true,
                tenantId = tenant.Id,
                tenantName = tenant.User?.FullName ?? "Unknown"
            });
        }

        // --- COMPLAINTS ---

        [HttpGet]
        public async Task<IActionResult> Complaints()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var complaints = await _context.Complaints
                // 1. Load Tenant and their User Profile (Fixes the missing name)
                .Include(c => c.Tenant)
                    .ThenInclude(t => t.User)

                // 2. Load Apartment and Building info (For the unit number/building name)
                .Include(c => c.Tenant)
                    .ThenInclude(t => t.Apartment)
                    .ThenInclude(a => a.Building)

                // 3. Filter by Owner
                .Where(c => c.Tenant.Apartment.Building.OwnerId == user.Id)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(complaints);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateComplaintStatus(int id, ComplaintStatus status, string? adminResponse)
        {
            var complaint = await _context.Complaints.FindAsync(id);
            if (complaint == null) return NotFound();

            complaint.Status = status;
            complaint.AdminResponse = adminResponse;
            if (status == ComplaintStatus.Resolved)
                complaint.ResolvedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Complaints));
        }

        // --- VENUE BOOKINGS ---

        [HttpGet]
        public async Task<IActionResult> VenueBookings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var bookings = await _context.VenueBookings
                .Include(v => v.Tenant)
                .ThenInclude(t => t.Apartment)
                .ThenInclude(a => a.Building)
                .Where(v => v.Tenant.Apartment.Building.OwnerId == user.Id)
                .OrderBy(v => v.BookingDate)
                .ThenBy(v => v.BookingTime)
                .ToListAsync();

            return View(bookings);
        }

        // POST: Owner/ApproveVenueBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveVenueBooking(int id, bool cleaningScheduled, string? adminNotes)
        {
            var booking = await _context.VenueBookings
                .Include(b => b.Tenant)
                .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            // 1. Update Status
            booking.Status = VenueBookingStatus.Approved;
            booking.CleaningScheduled = cleaningScheduled;
            booking.AdminNotes = adminNotes;

            // 2. Calculate "Unavailable Time" (1 Hour Buffer)
            // Assuming BookingTime is a TimeSpan and Duration is int (Hours). 
            // If Duration is missing in your model, I default to 3 hours here.
            int duration = 3;

            DateTime startDateTime = booking.BookingDate.Date + booking.BookingTime;
            DateTime endDateTime = startDateTime.AddHours(duration);

            DateTime bufferStart = startDateTime.AddHours(-1); // 1 Hour Before
            DateTime bufferEnd = endDateTime.AddHours(1);      // 1 Hour After

            // 3. Send Notification Logic (Simulation)
            // In a real app, you would save this to a Notifications table or use SignalR
            string notificationMessage = $"NOTICE: The {booking.VenueType} is unavailable on {booking.BookingDate:MMM dd} from {bufferStart:hh:mm tt} to {bufferEnd:hh:mm tt} due to a private event and cleaning.";

            // TODO: _notificationService.SendToAllTenants(notificationMessage);

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Booking Approved. Notification sent: {notificationMessage}";
            return RedirectToAction(nameof(VenueBookings));
        }

        // POST: Owner/RejectVenueBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectVenueBooking(int id, string? adminNotes)
        {
            var booking = await _context.VenueBookings.FindAsync(id);
            if (booking == null) return NotFound();

            booking.Status = VenueBookingStatus.Rejected;
            booking.AdminNotes = adminNotes;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Venue booking rejected.";
            return RedirectToAction(nameof(VenueBookings));
        }

        // GET: Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var tenant = await _context.Tenants.Include(t => t.User).FirstOrDefaultAsync(x => x.Id == id);
            if (tenant == null) return NotFound();

            // Populate Apartment Dropdown
            ViewData["ApartmentId"] = new SelectList(_context.Apartments
                .Select(a => new { a.Id, Name = "Unit " + a.ApartmentNumber + " (" + a.Building.Name + ")" }),
                "Id", "Name", tenant.ApartmentId);

            return View(tenant);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Tenant tenant)
        {
            if (id != tenant.Id) return NotFound();

            // 1. Fetch the EXISTING data from the database
            var tenantInDb = await _context.Tenants
                .Include(t => t.User)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (tenantInDb == null) return NotFound();

            if (ModelState.IsValid)
            {
                // 2. Update TENANT specific fields (Personal Details)
                tenantInDb.CNIC = tenant.CNIC;
                tenantInDb.PermanentAddress = tenant.PermanentAddress;
                tenantInDb.Notes = tenant.Notes;

                // 3. Update USER specific fields (Identity Data)
                // We read from tenant.User because the form submitted those values
                if (tenantInDb.User != null && tenant.User != null)
                {
                    tenantInDb.User.FullName = tenant.User.FullName;
                    tenantInDb.User.PhoneNumber = tenant.User.PhoneNumber;
                    tenantInDb.User.Email = tenant.User.Email;
                    tenantInDb.User.UserName = tenant.User.Email; // Optional: Keep username synced with email

                    // Update the User table explicitly
                    await _userManager.UpdateAsync(tenantInDb.User);
                }

                // 4. Save Tenant changes (Lease details in DB remain untouched)
                _context.Update(tenantInDb);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Tenants));
            }

            // If validation fails, reload dropdowns
            ViewData["ApartmentId"] = new SelectList(_context.Apartments, "Id", "ApartmentNumber", tenant.ApartmentId);
            return View(tenant);
        }

        // POST: Delete (Soft Delete)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant != null)
            {
                // 1. Expire the contract immediately (Soft Delete)
                tenant.ContractEndDate = DateTime.Today.AddDays(-1);

                // 2. Remove "Tenant" Role logic
                if (!string.IsNullOrEmpty(tenant.UserId))
                {
                    var user = await _userManager.FindByIdAsync(tenant.UserId);
                    if (user != null && await _userManager.IsInRoleAsync(user, "Tenant"))
                    {
                        await _userManager.RemoveFromRoleAsync(user, "Tenant");
                        await _userManager.AddToRoleAsync(user, "User");
                    }
                }

                _context.Tenants.Update(tenant);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Tenants));
        }
        // GET: Owner/Apartments
        [HttpGet]
        public async Task<IActionResult> Apartments()
        {
            var user = await _userManager.GetUserAsync(User);
            var apartments = await _context.Apartments
                .Include(a => a.Building)
                .Where(a => a.Building.OwnerId == user.Id)
                .OrderBy(a => a.Building.Name)
                .ThenBy(a => a.ApartmentNumber)
                .ToListAsync();

            return View(apartments);
        }

        // POST: Owner/EditApartment (Called from Modal)
        // GET: Owner/EditApartment/5
        [HttpGet]
        public async Task<IActionResult> EditApartment(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // 1. Fetch apartment with building info to verify owner
            var apartment = await _context.Apartments
                .Include(a => a.Building)
                .FirstOrDefaultAsync(a => a.Id == id && a.Building.OwnerId == user.Id);

            if (apartment == null) return NotFound();

            // 2. Deserialize existing photos
            var existingPhotos = string.IsNullOrEmpty(apartment.Photos)
                ? new List<string>()
                : JsonConvert.DeserializeObject<List<string>>(apartment.Photos) ?? new List<string>();

            // 3. Map to AddApartmentViewModel
            var viewModel = new AddApartmentViewModel
            {
                Id = apartment.Id,
                BuildingId = apartment.BuildingId,
                ApartmentNumber = apartment.ApartmentNumber,
                Type = apartment.Type,
                FloorNumber = apartment.FloorNumber,
                Size = apartment.Size,
                BaseRentRate = apartment.BaseRentRate,
                Description = apartment.Description,
                ExistingPhotos = existingPhotos
            };

            return View(viewModel);
        }

        // POST: Owner/EditApartment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditApartment(int id, AddApartmentViewModel viewModel, List<IFormFile> newPhotos, List<string> photosToKeep)
        {
            if (id != viewModel.Id) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // 1. Fetch original apartment to verify owner and compare photos
            var apartmentToUpdate = await _context.Apartments
                .Include(a => a.Building)
                .FirstOrDefaultAsync(a => a.Id == id && a.Building.OwnerId == user.Id);

            if (apartmentToUpdate == null) return NotFound();

            // 2. MANUAL VALIDATION: Ensure at least 1 photo remains (Existing + New)
            int existingCount = photosToKeep?.Count ?? 0;
            int newCount = newPhotos?.Count ?? 0;

            if (existingCount + newCount == 0)
            {
                ModelState.AddModelError("ExistingPhotos", "An apartment must have at least one photo.");
            }

            if (ModelState.IsValid)
            {
                // --- PHOTO MANAGEMENT LOGIC ---

                // A. Get original list from DB
                var originalPhotosList = string.IsNullOrEmpty(apartmentToUpdate.Photos)
                     ? new List<string>()
                     : JsonConvert.DeserializeObject<List<string>>(apartmentToUpdate.Photos) ?? new List<string>();

                // B. Determine which files to DELETE (Originals NOT in photosToKeep)
                photosToKeep ??= new List<string>();
                var photosToDelete = originalPhotosList.Except(photosToKeep).ToList();

                // C. Delete physical files
                foreach (var photoPath in photosToDelete)
                {
                    var physicalPath = Path.Combine(_hostEnvironment.WebRootPath, photoPath.TrimStart('/'));
                    if (System.IO.File.Exists(physicalPath))
                    {
                        System.IO.File.Delete(physicalPath);
                    }
                }

                // D. Process NEW uploads
                var finalPhotoList = new List<string>(photosToKeep); // Start with kept photos

                if (newPhotos != null && newPhotos.Count > 0)
                {
                    // Filter duplicates/AVIF
                    newPhotos = newPhotos.Where(p => !p.FileName.EndsWith(".avif") && !p.FileName.EndsWith(".heic")).ToList();

                    var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "apartments", apartmentToUpdate.Id.ToString());
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    foreach (var photo in newPhotos)
                    {
                        if (photo.Length > 0)
                        {
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
                            var filePath = Path.Combine(uploadsFolder, fileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await photo.CopyToAsync(stream);
                            }
                            finalPhotoList.Add($"/uploads/apartments/{apartmentToUpdate.Id}/{fileName}");
                        }
                    }
                }

                // --- UPDATE DATABASE ---
                apartmentToUpdate.ApartmentNumber = viewModel.ApartmentNumber;
                apartmentToUpdate.FloorNumber = viewModel.FloorNumber ?? 0;
                apartmentToUpdate.Type = viewModel.Type ?? 0;
                apartmentToUpdate.Size = viewModel.Size ?? 0;
                apartmentToUpdate.BaseRentRate = viewModel.BaseRentRate ?? 0;
                apartmentToUpdate.Description = viewModel.Description;
                apartmentToUpdate.Photos = JsonConvert.SerializeObject(finalPhotoList);

                _context.Update(apartmentToUpdate);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Apartment updated successfully.";
                return RedirectToAction(nameof(Apartments));
            }

            // If validation fails, reload existing photos for the view
            viewModel.ExistingPhotos = string.IsNullOrEmpty(apartmentToUpdate.Photos)
                ? new List<string>()
                : JsonConvert.DeserializeObject<List<string>>(apartmentToUpdate.Photos) ?? new List<string>();

            return View(viewModel);
        }

        // POST: Owner/DeleteApartment (Called from Modal)
        // POST: Owner/DeleteApartment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteApartment(int id)
        {
            // 1. Get Logged-in User
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // 2. Find Apartment (Ensure ownership)
            var apartment = await _context.Apartments
                .Include(a => a.Building)
                .FirstOrDefaultAsync(a => a.Id == id && a.Building.OwnerId == user.Id);

            if (apartment != null)
            {
                // 3. Safety Check: Prevent deleting active rentals
                if (apartment.Status == ApartmentStatus.Rented)
                {
                    TempData["Error"] = "Cannot delete an occupied apartment. Please remove the tenant first.";
                    return RedirectToAction(nameof(Apartments));
                }

                // 4. CLEANUP: Delete physical photos from server
                // This assumes your AddApartment logic saved photos to: wwwroot/uploads/apartments/{id}/
                if (_hostEnvironment.WebRootPath != null)
                {
                    var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "apartments", apartment.Id.ToString());
                    if (Directory.Exists(uploadsFolder))
                    {
                        try 
                        {
                            Directory.Delete(uploadsFolder, true); // true = recursive (delete files inside)
                        }
                        catch 
                        {
                            // Log error if needed, but don't stop the DB delete
                        }
                    }
                }

                // 5. HARD DELETE: Remove from Database
                _context.Apartments.Remove(apartment);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Apartment deleted permanently.";
            }
            else
            {
                TempData["Error"] = "Apartment not found or access denied.";
            }

            return RedirectToAction(nameof(Apartments));
        }

    }
}
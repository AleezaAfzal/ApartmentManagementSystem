using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ApartmentManagement.Models;
using ApartmentManagement.ViewModels;
using ApartmentManagement.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;

namespace ApartmentManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment hostEnvironment,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _hostEnvironment = hostEnvironment;
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            ViewData["PageType"] = "Auth";

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    CNIC = model.CNIC,
                    CreatedAt = DateTime.Now
                };

                if (model.CNICDocument != null)
                {
                    var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "documents");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.CNICDocument.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.CNICDocument.CopyToAsync(fileStream);
                    }
                    user.CNICDocumentPath = "/uploads/documents/" + uniqueFileName;
                }

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    string role = model.IsOwner ? "Owner" : "User";
                    await _userManager.AddToRoleAsync(user, role);

                    if (model.IsOwner && !string.IsNullOrEmpty(model.BuildingName))
                    {
                        var building = new Building
                        {
                            Name = model.BuildingName,
                            FullAddress = model.BuildingAddress ?? "",
                            City = "",
                            NumberOfFloors = 1,
                            OwnerId = user.Id,
                            CreatedAt = DateTime.Now
                        };
                        _context.Buildings.Add(building);
                        await _context.SaveChangesAsync();
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["PageType"] = "Auth";
           
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user != null)
                    {
                        var roles = await _userManager.GetRolesAsync(user);
                        
                        if (roles.Contains("Owner"))
                        {
                            return RedirectToAction("Dashboard", "Owner");
                        }
                        else if (roles.Contains("Tenant"))
                        {
                            return RedirectToAction("Dashboard", "Tenant");
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}


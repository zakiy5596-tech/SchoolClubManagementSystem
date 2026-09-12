using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _logger;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IActivityLogger logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            if (user.Status == UserStatus.Inactive)
            {
                ModelState.AddModelError(string.Empty, "Your account is inactive. Please contact the administrator.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, isPersistent: false, lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            await _logger.LogAsync(user.Id, user.FullName, "Login", $"{user.FullName} logged in.");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _logger.LogAsync(user.Id, user.FullName, "Logout", $"{user.FullName} logged out.");
            }
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var model = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? string.Empty
            };

            if (roles.Contains(Roles.Student))
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (student != null)
                {
                    model.StudentNumber = student.StudentNumber;
                    model.Phone = student.Phone;
                }
            }
            else if (roles.Contains(Roles.Advisor))
            {
                var advisor = await _context.Advisors.FirstOrDefaultAsync(a => a.UserId == user.Id);
                if (advisor != null)
                {
                    model.Phone = advisor.Phone;
                }
            }

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            model.Email = user.Email ?? string.Empty;
            model.Role = roles.FirstOrDefault() ?? string.Empty;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            user.FullName = model.FullName;
            await _userManager.UpdateAsync(user);

            if (roles.Contains(Roles.Student))
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
                if (student != null)
                {
                    student.Phone = model.Phone;
                    model.StudentNumber = student.StudentNumber;
                }
            }
            else if (roles.Contains(Roles.Advisor))
            {
                var advisor = await _context.Advisors.FirstOrDefaultAsync(a => a.UserId == user.Id);
                if (advisor != null)
                {
                    advisor.Phone = model.Phone;
                }
            }

            await _context.SaveChangesAsync();
            await _logger.LogAsync(user.Id, user.FullName, "Profile Update", $"{user.FullName} updated their profile.");

            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction("Profile");
        }
    }
}

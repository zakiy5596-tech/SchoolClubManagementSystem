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
    [Authorize(Roles = Roles.Admin)]
    public class AdvisorsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _logger;

        public AdvisorsController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IActivityLogger logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search, string? status)
        {
            var query = _context.Advisors.Include(a => a.User).Include(a => a.Clubs).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(a => a.User!.FullName.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<UserStatus>(status, out var statusEnum))
            {
                query = query.Where(a => a.User!.Status == statusEnum);
            }

            var advisors = await query.OrderBy(a => a.User!.FullName).ToListAsync();

            ViewData["Search"] = search;
            ViewData["StatusFilter"] = status;
            ViewData["ReportTitle"] = "Advisors Report";

            return View(advisors);
        }

        public IActionResult Create()
        {
            return View(new AdvisorFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdvisorFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError(nameof(model.Email), "This email is already registered.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                FullName = model.FullName,
                Status = model.Status,
                CreatedAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, model.Password!);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, Roles.Advisor);

            _context.Advisors.Add(new Advisor
            {
                UserId = user.Id,
                Phone = model.Phone
            });
            await _context.SaveChangesAsync();

            await _logger.LogAsync(_userManager.GetUserId(User), User.Identity!.Name!, "Advisor Created", $"Created advisor {user.FullName}.");

            TempData["Success"] = "Advisor created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var advisor = await _context.Advisors.Include(a => a.User).FirstOrDefaultAsync(a => a.AdvisorId == id);
            if (advisor == null || advisor.User == null) return NotFound();

            var model = new AdvisorFormViewModel
            {
                AdvisorId = advisor.AdvisorId,
                FullName = advisor.User.FullName,
                Email = advisor.User.Email ?? string.Empty,
                Phone = advisor.Phone,
                Status = advisor.User.Status
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AdvisorFormViewModel model)
        {
            if (id != model.AdvisorId) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var advisor = await _context.Advisors.Include(a => a.User).FirstOrDefaultAsync(a => a.AdvisorId == id);
            if (advisor == null || advisor.User == null) return NotFound();

            var emailOwner = await _userManager.FindByEmailAsync(model.Email);
            if (emailOwner != null && emailOwner.Id != advisor.UserId)
            {
                ModelState.AddModelError(nameof(model.Email), "This email is already registered.");
                return View(model);
            }

            advisor.User.FullName = model.FullName;
            advisor.User.Email = model.Email;
            advisor.User.UserName = model.Email;
            advisor.User.Status = model.Status;
            advisor.Phone = model.Phone;

            await _userManager.UpdateAsync(advisor.User);

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(advisor.User);
                await _userManager.ResetPasswordAsync(advisor.User, token, model.Password);
            }

            await _context.SaveChangesAsync();
            await _logger.LogAsync(_userManager.GetUserId(User), User.Identity!.Name!, "Advisor Updated", $"Updated advisor {advisor.User.FullName}.");

            TempData["Success"] = "Advisor updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var advisor = await _context.Advisors.Include(a => a.User).FirstOrDefaultAsync(a => a.AdvisorId == id);
            if (advisor == null || advisor.User == null) return NotFound();

            advisor.User.Status = advisor.User.Status == UserStatus.Active ? UserStatus.Inactive : UserStatus.Active;
            await _userManager.UpdateAsync(advisor.User);

            await _logger.LogAsync(_userManager.GetUserId(User), User.Identity!.Name!, "Advisor Status Changed", $"{advisor.User.FullName} set to {advisor.User.Status}.");

            TempData["Success"] = $"Advisor {(advisor.User.Status == UserStatus.Active ? "activated" : "deactivated")} successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}

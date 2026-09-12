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
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _logger;

        public UsersController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IActivityLogger logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search, string? role, string? status)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.FullName.Contains(search) || u.Email!.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<UserStatus>(status, out var statusEnum))
            {
                query = query.Where(u => u.Status == statusEnum);
            }

            var users = await query.OrderBy(u => u.FullName).ToListAsync();

            var userRoles = new Dictionary<string, string>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                userRoles[u.Id] = roles.FirstOrDefault() ?? string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                users = users.Where(u => userRoles[u.Id] == role).ToList();
            }

            ViewData["UserRoles"] = userRoles;
            ViewData["Search"] = search;
            ViewData["RoleFilter"] = role;
            ViewData["StatusFilter"] = status;
            ViewData["ReportTitle"] = "Users Report";

            return View(users);
        }

        public IActionResult Create()
        {
            return View(new UserFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
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

            await _userManager.AddToRoleAsync(user, model.Role);
            await _logger.LogAsync(_userManager.GetUserId(User), User.Identity!.Name!, "User Created", $"Created user {user.FullName} ({model.Role}).");

            TempData["Success"] = "User created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var model = new UserFormViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? string.Empty,
                Status = user.Status
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserFormViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var emailOwner = await _userManager.FindByEmailAsync(model.Email);
            if (emailOwner != null && emailOwner.Id != id)
            {
                ModelState.AddModelError(nameof(model.Email), "This email is already registered.");
                return View(model);
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.Status = model.Status;
            await _userManager.UpdateAsync(user);

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(model.Role))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, model.Role);
            }

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, token, model.Password);
            }

            await _logger.LogAsync(_userManager.GetUserId(User), User.Identity!.Name!, "User Updated", $"Updated user {user.FullName}.");

            TempData["Success"] = "User updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.Status = user.Status == UserStatus.Active ? UserStatus.Inactive : UserStatus.Active;
            await _userManager.UpdateAsync(user);

            await _logger.LogAsync(_userManager.GetUserId(User), User.Identity!.Name!, "User Status Changed", $"{user.FullName} set to {user.Status}.");

            TempData["Success"] = $"User {(user.Status == UserStatus.Active ? "activated" : "deactivated")} successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}

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
    public class ParentsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _logger;

        public ParentsController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IActivityLogger logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search, string? status)
        {
            var query = _context.Parents
                .Include(p => p.User)
                .Include(p => p.Children).ThenInclude(c => c.Student).ThenInclude(s => s!.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.User!.FullName.Contains(search));
            }
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<UserStatus>(status, out var statusEnum))
            {
                query = query.Where(p => p.User!.Status == statusEnum);
            }

            var parents = await query.OrderBy(p => p.User!.FullName).ToListAsync();

            ViewData["Search"] = search;
            ViewData["StatusFilter"] = status;
            ViewData["ReportTitle"] = "Parents Report";

            return View(parents);
        }

        public async Task<IActionResult> Create()
        {
            var model = new ParentFormViewModel
            {
                AllStudents = await _context.Students.Include(s => s.User).OrderBy(s => s.User!.FullName).ToListAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ParentFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AllStudents = await _context.Students.Include(s => s.User).OrderBy(s => s.User!.FullName).ToListAsync();
                return View(model);
            }

            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError(nameof(model.Email), "This email is already registered.");
                model.AllStudents = await _context.Students.Include(s => s.User).OrderBy(s => s.User!.FullName).ToListAsync();
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
                model.AllStudents = await _context.Students.Include(s => s.User).OrderBy(s => s.User!.FullName).ToListAsync();
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, Roles.Parent);

            var parent = new Parent { UserId = user.Id, Phone = model.Phone };
            _context.Parents.Add(parent);
            await _context.SaveChangesAsync();

            foreach (var studentId in model.SelectedStudentIds.Distinct())
            {
                _context.ParentStudents.Add(new ParentStudent { ParentId = parent.ParentId, StudentId = studentId });
            }
            await _context.SaveChangesAsync();

            await _logger.LogAsync(_userManager.GetUserId(User), User.Identity!.Name!, "Parent Created", $"Created parent {user.FullName}.");

            TempData["Success"] = "Parent account created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var parent = await _context.Parents
                .Include(p => p.User)
                .Include(p => p.Children)
                .FirstOrDefaultAsync(p => p.ParentId == id);
            if (parent == null || parent.User == null) return NotFound();

            var model = new ParentFormViewModel
            {
                ParentId = parent.ParentId,
                FullName = parent.User.FullName,
                Email = parent.User.Email ?? string.Empty,
                Phone = parent.Phone,
                Status = parent.User.Status,
                SelectedStudentIds = parent.Children.Select(c => c.StudentId).ToList(),
                AllStudents = await _context.Students.Include(s => s.User).OrderBy(s => s.User!.FullName).ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ParentFormViewModel model)
        {
            if (id != model.ParentId) return NotFound();

            if (!ModelState.IsValid)
            {
                model.AllStudents = await _context.Students.Include(s => s.User).OrderBy(s => s.User!.FullName).ToListAsync();
                return View(model);
            }

            var parent = await _context.Parents.Include(p => p.User).Include(p => p.Children).FirstOrDefaultAsync(p => p.ParentId == id);
            if (parent == null || parent.User == null) return NotFound();

            var emailOwner = await _userManager.FindByEmailAsync(model.Email);
            if (emailOwner != null && emailOwner.Id != parent.UserId)
            {
                ModelState.AddModelError(nameof(model.Email), "This email is already registered.");
                model.AllStudents = await _context.Students.Include(s => s.User).OrderBy(s => s.User!.FullName).ToListAsync();
                return View(model);
            }

            parent.User.FullName = model.FullName;
            parent.User.Email = model.Email;
            parent.User.UserName = model.Email;
            parent.User.Status = model.Status;
            parent.Phone = model.Phone;
            await _userManager.UpdateAsync(parent.User);

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(parent.User);
                await _userManager.ResetPasswordAsync(parent.User, token, model.Password);
            }

            var currentStudentIds = parent.Children.Select(c => c.StudentId).ToHashSet();
            var newStudentIds = model.SelectedStudentIds.Distinct().ToHashSet();

            foreach (var link in parent.Children.Where(c => !newStudentIds.Contains(c.StudentId)).ToList())
            {
                _context.ParentStudents.Remove(link);
            }
            foreach (var studentId in newStudentIds.Where(sid => !currentStudentIds.Contains(sid)))
            {
                _context.ParentStudents.Add(new ParentStudent { ParentId = parent.ParentId, StudentId = studentId });
            }

            await _context.SaveChangesAsync();
            await _logger.LogAsync(_userManager.GetUserId(User), User.Identity!.Name!, "Parent Updated", $"Updated parent {parent.User.FullName}.");

            TempData["Success"] = "Parent updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var parent = await _context.Parents.Include(p => p.User).FirstOrDefaultAsync(p => p.ParentId == id);
            if (parent == null || parent.User == null) return NotFound();

            parent.User.Status = parent.User.Status == UserStatus.Active ? UserStatus.Inactive : UserStatus.Active;
            await _userManager.UpdateAsync(parent.User);

            await _logger.LogAsync(_userManager.GetUserId(User), User.Identity!.Name!, "Parent Status Changed", $"{parent.User.FullName} set to {parent.User.Status}.");

            TempData["Success"] = $"Parent {(parent.User.Status == UserStatus.Active ? "activated" : "deactivated")} successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}

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
    public class StudentsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IActivityLogger _logger;

        public StudentsController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IActivityLogger logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search, string? status)
        {
            var query = _context.Students.Include(s => s.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s => s.User!.FullName.Contains(search) || s.StudentNumber.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<UserStatus>(status, out var statusEnum))
            {
                query = query.Where(s => s.User!.Status == statusEnum);
            }

            var students = await query.OrderBy(s => s.User!.FullName).ToListAsync();

            ViewData["Search"] = search;
            ViewData["StatusFilter"] = status;
            ViewData["ReportTitle"] = "Students Report";

            return View(students);
        }

        public async Task<IActionResult> Details(int id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Memberships).ThenInclude(m => m.Club)
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null) return NotFound();
            return View(student);
        }

        public IActionResult Create()
        {
            return View(new StudentFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentFormViewModel model)
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

            if (await _context.Students.AnyAsync(s => s.StudentNumber == model.StudentNumber))
            {
                ModelState.AddModelError(nameof(model.StudentNumber), "This student number is already in use.");
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

            await _userManager.AddToRoleAsync(user, Roles.Student);

            _context.Students.Add(new Student
            {
                UserId = user.Id,
                StudentNumber = model.StudentNumber,
                Phone = model.Phone
            });
            await _context.SaveChangesAsync();

            await _logger.LogAsync(_userManager.GetUserId(User), User.Identity!.Name!, "Student Created", $"Created student {user.FullName} ({model.StudentNumber}).");

            TempData["Success"] = "Student created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.StudentId == id);
            if (student == null || student.User == null) return NotFound();

            var model = new StudentFormViewModel
            {
                StudentId = student.StudentId,
                FullName = student.User.FullName,
                Email = student.User.Email ?? string.Empty,
                StudentNumber = student.StudentNumber,
                Phone = student.Phone,
                Status = student.User.Status
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StudentFormViewModel model)
        {
            if (id != model.StudentId) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.StudentId == id);
            if (student == null || student.User == null) return NotFound();

            var emailOwner = await _userManager.FindByEmailAsync(model.Email);
            if (emailOwner != null && emailOwner.Id != student.UserId)
            {
                ModelState.AddModelError(nameof(model.Email), "This email is already registered.");
                return View(model);
            }

            if (await _context.Students.AnyAsync(s => s.StudentNumber == model.StudentNumber && s.StudentId != id))
            {
                ModelState.AddModelError(nameof(model.StudentNumber), "This student number is already in use.");
                return View(model);
            }

            student.User.FullName = model.FullName;
            student.User.Email = model.Email;
            student.User.UserName = model.Email;
            student.User.Status = model.Status;
            student.StudentNumber = model.StudentNumber;
            student.Phone = model.Phone;

            await _userManager.UpdateAsync(student.User);

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(student.User);
                await _userManager.ResetPasswordAsync(student.User, token, model.Password);
            }

            await _context.SaveChangesAsync();
            await _logger.LogAsync(_userManager.GetUserId(User), User.Identity!.Name!, "Student Updated", $"Updated student {student.User.FullName}.");

            TempData["Success"] = "Student updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.StudentId == id);
            if (student == null || student.User == null) return NotFound();

            student.User.Status = student.User.Status == UserStatus.Active ? UserStatus.Inactive : UserStatus.Active;
            await _userManager.UpdateAsync(student.User);

            await _logger.LogAsync(_userManager.GetUserId(User), User.Identity!.Name!, "Student Status Changed", $"{student.User.FullName} set to {student.User.Status}.");

            TempData["Success"] = $"Student {(student.User.Status == UserStatus.Active ? "activated" : "deactivated")} successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}

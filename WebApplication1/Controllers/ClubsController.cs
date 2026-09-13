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
    [Authorize]
    public class ClubsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IActivityLogger _logger;

        public ClubsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IActivityLogger logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Index(string? search, string? category, int? advisorId)
        {
            var query = _context.Clubs.Include(c => c.Advisor).ThenInclude(a => a!.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.ClubName.Contains(search));
            }
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(c => c.Category == category);
            }
            if (advisorId.HasValue)
            {
                query = query.Where(c => c.AdvisorId == advisorId.Value);
            }

            var clubs = await query.OrderBy(c => c.ClubName).ToListAsync();

            ViewData["Search"] = search;
            ViewData["CategoryFilter"] = category;
            ViewData["AdvisorFilter"] = advisorId;
            ViewData["Categories"] = await _context.Clubs.Select(c => c.Category).Distinct().OrderBy(c => c).ToListAsync();
            ViewData["AdvisorList"] = await _context.Advisors.Include(a => a.User).OrderBy(a => a.User!.FullName).ToListAsync();
            ViewData["ReportTitle"] = "Clubs Report";

            return View(clubs);
        }

        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Create()
        {
            var model = new ClubFormViewModel
            {
                Advisors = await _context.Advisors.Include(a => a.User).OrderBy(a => a.User!.FullName).ToListAsync()
            };
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClubFormViewModel model)
        {
            if (await _context.Clubs.AnyAsync(c => c.ClubName == model.ClubName))
            {
                ModelState.AddModelError(nameof(model.ClubName), "A club with this name already exists.");
            }

            if (!ModelState.IsValid)
            {
                model.Advisors = await _context.Advisors.Include(a => a.User).OrderBy(a => a.User!.FullName).ToListAsync();
                return View(model);
            }

            var club = new Club
            {
                ClubName = model.ClubName,
                Description = model.Description,
                Category = model.Category,
                AdvisorId = model.AdvisorId,
                DateCreated = DateTime.Now
            };
            _context.Clubs.Add(club);
            await _context.SaveChangesAsync();

            await _logger.LogAsync(_userManager.GetUserId(User), User.Identity!.Name!, "Club Created", $"Created club {club.ClubName}.");

            TempData["Success"] = "Club created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Edit(int id)
        {
            var club = await _context.Clubs.FindAsync(id);
            if (club == null) return NotFound();

            var model = new ClubFormViewModel
            {
                ClubId = club.ClubId,
                ClubName = club.ClubName,
                Description = club.Description,
                Category = club.Category,
                AdvisorId = club.AdvisorId,
                Advisors = await _context.Advisors.Include(a => a.User).OrderBy(a => a.User!.FullName).ToListAsync()
            };
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ClubFormViewModel model)
        {
            if (id != model.ClubId) return NotFound();

            if (await _context.Clubs.AnyAsync(c => c.ClubName == model.ClubName && c.ClubId != id))
            {
                ModelState.AddModelError(nameof(model.ClubName), "A club with this name already exists.");
            }

            if (!ModelState.IsValid)
            {
                model.Advisors = await _context.Advisors.Include(a => a.User).OrderBy(a => a.User!.FullName).ToListAsync();
                return View(model);
            }

            var club = await _context.Clubs.FindAsync(id);
            if (club == null) return NotFound();

            club.ClubName = model.ClubName;
            club.Description = model.Description;
            club.Category = model.Category;
            club.AdvisorId = model.AdvisorId;
            await _context.SaveChangesAsync();

            await _logger.LogAsync(_userManager.GetUserId(User), User.Identity!.Name!, "Club Updated", $"Updated club {club.ClubName}.");

            TempData["Success"] = "Club updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var club = await _context.Clubs.Include(c => c.Advisor).ThenInclude(a => a!.User).FirstOrDefaultAsync(c => c.ClubId == id);
            if (club == null) return NotFound();
            return View(club);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = Roles.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var club = await _context.Clubs.FindAsync(id);
            if (club == null) return NotFound();

            _context.Clubs.Remove(club);
            await _context.SaveChangesAsync();

            await _logger.LogAsync(_userManager.GetUserId(User), User.Identity!.Name!, "Club Deleted", $"Deleted club {club.ClubName}.");

            TempData["Success"] = "Club deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = $"{Roles.Advisor},{Roles.Student},{Roles.Parent}")]
        public async Task<IActionResult> MyClubs()
        {
            var userId = _userManager.GetUserId(User);

            if (User.IsInRole(Roles.Advisor))
            {
                var advisor = await _context.Advisors.FirstOrDefaultAsync(a => a.UserId == userId);
                if (advisor == null) return View("AdvisorMyClubs", new List<Club>());

                var clubs = await _context.Clubs
                    .Where(c => c.AdvisorId == advisor.AdvisorId)
                    .Include(c => c.Memberships)
                    .Include(c => c.Events)
                    .ToListAsync();

                return View("AdvisorMyClubs", clubs);
            }
            else if (User.IsInRole(Roles.Student))
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
                if (student == null) return View("StudentMyClubs", new List<ClubMembership>());

                var memberships = await _context.ClubMemberships
                    .Where(m => m.StudentId == student.StudentId)
                    .Include(m => m.Club).ThenInclude(c => c!.Advisor).ThenInclude(a => a!.User)
                    .ToListAsync();

                return View("StudentMyClubs", memberships);
            }
            else
            {
                var parent = await _context.Parents.Include(p => p.Children).FirstOrDefaultAsync(p => p.UserId == userId);
                if (parent == null) return View("ParentMyClubs", new List<ClubMembership>());

                var childIds = parent.Children.Select(c => c.StudentId).ToList();
                var memberships = await _context.ClubMemberships
                    .Where(m => childIds.Contains(m.StudentId))
                    .Include(m => m.Student).ThenInclude(s => s!.User)
                    .Include(m => m.Club).ThenInclude(c => c!.Advisor).ThenInclude(a => a!.User)
                    .OrderBy(m => m.Student!.User!.FullName)
                    .ToListAsync();

                return View("ParentMyClubs", memberships);
            }
        }

        [Authorize(Roles = $"{Roles.Student},{Roles.Parent}")]
        public async Task<IActionResult> Available(string? search, string? category)
        {
            var userId = _userManager.GetUserId(User);

            var query = _context.Clubs.Include(c => c.Advisor).ThenInclude(a => a!.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.ClubName.Contains(search));
            }
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(c => c.Category == category);
            }

            var clubs = await query.OrderBy(c => c.ClubName).ToListAsync();

            if (User.IsInRole(Roles.Student))
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
                if (student == null) return NotFound();

                var myMemberships = await _context.ClubMemberships
                    .Where(m => m.StudentId == student.StudentId)
                    .ToDictionaryAsync(m => m.ClubId, m => m.Status);

                ViewData["MyMemberships"] = myMemberships;
            }

            ViewData["Search"] = search;
            ViewData["CategoryFilter"] = category;
            ViewData["Categories"] = await _context.Clubs.Select(c => c.Category).Distinct().OrderBy(c => c).ToListAsync();

            return View(clubs);
        }

        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var club = await _context.Clubs
                .Include(c => c.Advisor).ThenInclude(a => a!.User)
                .Include(c => c.Memberships)
                .FirstOrDefaultAsync(c => c.ClubId == id);
            if (club == null) return NotFound();

            if (User.IsInRole(Roles.Student))
            {
                var userId = _userManager.GetUserId(User);
                var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
                var membership = student == null ? null : await _context.ClubMemberships
                    .FirstOrDefaultAsync(m => m.ClubId == id && m.StudentId == student.StudentId);
                ViewData["MyMembership"] = membership;
            }
            else if (User.IsInRole(Roles.Parent))
            {
                var userId = _userManager.GetUserId(User);
                var parent = await _context.Parents.Include(p => p.Children).ThenInclude(c => c.Student).ThenInclude(s => s!.User)
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                var children = parent?.Children.Select(c => c.Student!).ToList() ?? new List<Student>();
                var childIds = children.Select(c => c.StudentId).ToList();
                var childMemberships = await _context.ClubMemberships
                    .Where(m => m.ClubId == id && childIds.Contains(m.StudentId))
                    .ToDictionaryAsync(m => m.StudentId, m => m);

                ViewData["MyChildren"] = children;
                ViewData["ChildMemberships"] = childMemberships;
            }

            return View(club);
        }

        [Authorize(Roles = Roles.Student)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(int id)
        {
            var userId = _userManager.GetUserId(User);
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return NotFound();

            var club = await _context.Clubs.FindAsync(id);
            if (club == null) return NotFound();

            var existing = await _context.ClubMemberships
                .FirstOrDefaultAsync(m => m.ClubId == id && m.StudentId == student.StudentId && m.Status != MembershipStatus.Rejected);

            if (existing != null)
            {
                TempData["Error"] = existing.Status == MembershipStatus.Approved
                    ? "You are already a member of this club."
                    : "You already have a pending request for this club.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _context.ClubMemberships.Add(new ClubMembership
            {
                StudentId = student.StudentId,
                ClubId = id,
                Status = MembershipStatus.Pending,
                RequestedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            await _logger.LogAsync(userId, User.Identity!.Name!, "Membership Requested", $"Requested to join {club.ClubName}.");

            TempData["Success"] = "Membership request submitted successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = Roles.Parent)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinAsParent(int id, int studentId)
        {
            var userId = _userManager.GetUserId(User);
            var parent = await _context.Parents.Include(p => p.Children)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (parent == null || !parent.Children.Any(c => c.StudentId == studentId))
            {
                TempData["Error"] = "You are not authorized to register this student.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var club = await _context.Clubs.FindAsync(id);
            if (club == null) return NotFound();

            var existing = await _context.ClubMemberships
                .FirstOrDefaultAsync(m => m.ClubId == id && m.StudentId == studentId && m.Status != MembershipStatus.Rejected);

            if (existing != null)
            {
                TempData["Error"] = existing.Status == MembershipStatus.Approved
                    ? "This student is already a member of this club."
                    : "A membership request for this student is already pending for this club.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _context.ClubMemberships.Add(new ClubMembership
            {
                StudentId = studentId,
                ClubId = id,
                Status = MembershipStatus.Pending,
                RequestedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.StudentId == studentId);
            await _logger.LogAsync(userId, User.Identity!.Name!, "Membership Requested",
                $"Registered {student?.User?.FullName} for {club.ClubName}.");

            TempData["Success"] = "Membership request submitted successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = $"{Roles.Admin},{Roles.Advisor}")]
        public async Task<IActionResult> Members(int id, string? search, string? status)
        {
            var club = await _context.Clubs.Include(c => c.Advisor).FirstOrDefaultAsync(c => c.ClubId == id);
            if (club == null) return NotFound();

            if (User.IsInRole(Roles.Advisor))
            {
                var userId = _userManager.GetUserId(User);
                var advisor = await _context.Advisors.FirstOrDefaultAsync(a => a.UserId == userId);
                if (advisor == null || club.AdvisorId != advisor.AdvisorId) return Forbid();
            }

            var query = _context.ClubMemberships.Where(m => m.ClubId == id).Include(m => m.Student).ThenInclude(s => s!.User).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(m => m.Student!.User!.FullName.Contains(search) || m.Student!.StudentNumber.Contains(search));
            }
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<MembershipStatus>(status, out var statusEnum))
            {
                query = query.Where(m => m.Status == statusEnum);
            }

            var members = await query.OrderBy(m => m.Student!.User!.FullName).ToListAsync();

            ViewData["Club"] = club;
            ViewData["Search"] = search;
            ViewData["StatusFilter"] = status;
            ViewData["ReportTitle"] = $"{club.ClubName} - Member List";

            return View(members);
        }
    }
}

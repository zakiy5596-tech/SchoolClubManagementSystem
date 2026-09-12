using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = $"{Roles.Admin},{Roles.Advisor}")]
    public class MembershipsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IActivityLogger _logger;

        public MembershipsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IActivityLogger logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? status, int? clubId, string? search)
        {
            var query = _context.ClubMemberships
                .Include(m => m.Student).ThenInclude(s => s!.User)
                .Include(m => m.Club)
                .AsQueryable();

            List<Club> clubOptions;

            if (User.IsInRole(Roles.Advisor))
            {
                var userId = _userManager.GetUserId(User);
                var advisor = await _context.Advisors.FirstOrDefaultAsync(a => a.UserId == userId);
                var advisorClubIds = advisor == null
                    ? new List<int>()
                    : await _context.Clubs.Where(c => c.AdvisorId == advisor.AdvisorId).Select(c => c.ClubId).ToListAsync();

                query = query.Where(m => advisorClubIds.Contains(m.ClubId));
                clubOptions = await _context.Clubs.Where(c => advisorClubIds.Contains(c.ClubId)).ToListAsync();
            }
            else
            {
                clubOptions = await _context.Clubs.OrderBy(c => c.ClubName).ToListAsync();
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<MembershipStatus>(status, out var statusEnum))
            {
                query = query.Where(m => m.Status == statusEnum);
            }
            if (clubId.HasValue)
            {
                query = query.Where(m => m.ClubId == clubId.Value);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(m => m.Student!.User!.FullName.Contains(search) || m.Student!.StudentNumber.Contains(search));
            }

            var memberships = await query.OrderByDescending(m => m.RequestedAt).ToListAsync();

            ViewData["StatusFilter"] = status;
            ViewData["ClubFilter"] = clubId;
            ViewData["Search"] = search;
            ViewData["ClubOptions"] = clubOptions;
            ViewData["ReportTitle"] = "Membership Requests Report";

            return View(memberships);
        }

        private async Task<bool> CanManage(ClubMembership membership)
        {
            if (User.IsInRole(Roles.Admin)) return true;

            var userId = _userManager.GetUserId(User);
            var advisor = await _context.Advisors.FirstOrDefaultAsync(a => a.UserId == userId);
            return advisor != null && membership.Club != null && membership.Club.AdvisorId == advisor.AdvisorId;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var membership = await _context.ClubMemberships.Include(m => m.Club).Include(m => m.Student).ThenInclude(s => s!.User).FirstOrDefaultAsync(m => m.MembershipId == id);
            if (membership == null) return NotFound();
            if (!await CanManage(membership)) return Forbid();

            membership.Status = MembershipStatus.Approved;
            membership.DateJoined = DateTime.Now;
            await _context.SaveChangesAsync();

            await _logger.LogAsync(_userManager.GetUserId(User), User.Identity!.Name!, "Membership Approved",
                $"Approved {membership.Student?.User?.FullName}'s membership in {membership.Club?.ClubName}.");

            TempData["Success"] = "Membership request approved.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var membership = await _context.ClubMemberships.Include(m => m.Club).Include(m => m.Student).ThenInclude(s => s!.User).FirstOrDefaultAsync(m => m.MembershipId == id);
            if (membership == null) return NotFound();
            if (!await CanManage(membership)) return Forbid();

            membership.Status = MembershipStatus.Rejected;
            await _context.SaveChangesAsync();

            await _logger.LogAsync(_userManager.GetUserId(User), User.Identity!.Name!, "Membership Rejected",
                $"Rejected {membership.Student?.User?.FullName}'s membership in {membership.Club?.ClubName}.");

            TempData["Success"] = "Membership request rejected.";
            return RedirectToAction(nameof(Index));
        }
    }
}

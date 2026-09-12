using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = Roles.Advisor)]
    public class AdvisorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdvisorController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);
            var advisor = await _context.Advisors.FirstOrDefaultAsync(a => a.UserId == userId);

            var model = new AdvisorDashboardViewModel();
            if (advisor == null)
            {
                return View(model);
            }

            var clubIds = await _context.Clubs.Where(c => c.AdvisorId == advisor.AdvisorId).Select(c => c.ClubId).ToListAsync();

            model.AssignedClubs = await _context.Clubs.Where(c => clubIds.Contains(c.ClubId)).ToListAsync();
            model.TotalMembers = await _context.ClubMemberships.CountAsync(m => clubIds.Contains(m.ClubId) && m.Status == MembershipStatus.Approved);
            model.PendingMembershipRequests = await _context.ClubMemberships.CountAsync(m => clubIds.Contains(m.ClubId) && m.Status == MembershipStatus.Pending);

            var events = await _context.Events.Include(e => e.Club).Where(e => clubIds.Contains(e.ClubId)).ToListAsync();
            model.UpcomingActivities = events.Where(e => e.IsUpcoming).OrderBy(e => e.EventDate).Take(10).ToList();

            model.RecentLogs = await _context.ActivityLogs
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.Timestamp)
                .Take(10)
                .ToListAsync();

            return View(model);
        }
    }
}

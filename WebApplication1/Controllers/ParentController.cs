using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = Roles.Parent)]
    public class ParentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ParentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);
            var parent = await _context.Parents
                .Include(p => p.User)
                .Include(p => p.Children).ThenInclude(c => c.Student).ThenInclude(s => s!.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (parent == null) return NotFound();

            var children = parent.Children.Select(c => c.Student!).ToList();
            var childIds = children.Select(c => c.StudentId).ToList();

            var memberships = await _context.ClubMemberships
                .Where(m => childIds.Contains(m.StudentId))
                .Include(m => m.Student).ThenInclude(s => s!.User)
                .Include(m => m.Club)
                .ToListAsync();

            var approvedClubIds = memberships.Where(m => m.Status == MembershipStatus.Approved).Select(m => m.ClubId).Distinct().ToList();
            var events = await _context.Events.Include(e => e.Club).Where(e => approvedClubIds.Contains(e.ClubId)).ToListAsync();

            var model = new ParentDashboardViewModel
            {
                Parent = parent,
                Children = children,
                ChildMemberships = memberships,
                UpcomingActivities = events.Where(e => e.IsUpcoming).OrderBy(e => e.EventDate).Take(10).ToList()
            };

            return View(model);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var events = await _context.Events.ToListAsync();

            var model = new AdminDashboardViewModel
            {
                TotalStudents = await _context.Students.CountAsync(),
                TotalClubs = await _context.Clubs.CountAsync(),
                TotalAdvisors = await _context.Advisors.CountAsync(),
                PendingMembershipRequests = await _context.ClubMemberships.CountAsync(m => m.Status == MembershipStatus.Pending),
                UpcomingActivities = events.Count(e => e.IsUpcoming),
                RecentLogs = await _context.ActivityLogs.OrderByDescending(l => l.Timestamp).Take(15).ToListAsync()
            };

            return View(model);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Models.ViewModels;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = Roles.Student)]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);
            var student = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.UserId == userId);
            if (student == null) return NotFound();

            var myClubIds = await _context.ClubMemberships
                .Where(m => m.StudentId == student.StudentId && m.Status == MembershipStatus.Approved)
                .Select(m => m.ClubId)
                .ToListAsync();

            var events = await _context.Events.Include(e => e.Club).Where(e => myClubIds.Contains(e.ClubId)).ToListAsync();

            var model = new StudentDashboardViewModel
            {
                Student = student,
                MyClubs = await _context.ClubMemberships
                    .Where(m => m.StudentId == student.StudentId)
                    .Include(m => m.Club).ThenInclude(c => c!.Advisor).ThenInclude(a => a!.User)
                    .ToListAsync(),
                AvailableClubs = await _context.Clubs
                    .Where(c => !myClubIds.Contains(c.ClubId))
                    .Include(c => c.Advisor).ThenInclude(a => a!.User)
                    .OrderBy(c => c.ClubName)
                    .Take(6)
                    .ToListAsync(),
                UpcomingActivities = events.Where(e => e.IsUpcoming).OrderBy(e => e.EventDate).Take(10).ToList(),
                RecentPastActivities = events.Where(e => !e.IsUpcoming).OrderByDescending(e => e.EventDate).Take(5).ToList()
            };

            return View(model);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = $"{Roles.Admin},{Roles.Advisor}")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Students(string? search, string? status)
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
            ViewData["ReportTitle"] = "Student Report";
            return View(students);
        }

        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> ClubsReport(string? search, string? category)
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

            var clubs = await query.OrderBy(c => c.ClubName).ToListAsync();
            ViewData["Search"] = search;
            ViewData["CategoryFilter"] = category;
            ViewData["Categories"] = await _context.Clubs.Select(c => c.Category).Distinct().OrderBy(c => c).ToListAsync();
            ViewData["ReportTitle"] = "Club Report";
            return View(clubs);
        }

        public async Task<IActionResult> MembershipReport(string? status, int? clubId, string? search)
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
                var advisorClubIds = advisor == null ? new List<int>() :
                    await _context.Clubs.Where(c => c.AdvisorId == advisor.AdvisorId).Select(c => c.ClubId).ToListAsync();
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
                query = query.Where(m => m.Student!.User!.FullName.Contains(search));
            }

            var memberships = await query.OrderBy(m => m.Club!.ClubName).ToListAsync();

            ViewData["StatusFilter"] = status;
            ViewData["ClubFilter"] = clubId;
            ViewData["Search"] = search;
            ViewData["ClubOptions"] = clubOptions;
            ViewData["ReportTitle"] = "Membership Report";
            return View(memberships);
        }

        public async Task<IActionResult> ActivityReport(string? filter, int? clubId, string? search)
        {
            List<int> clubIds;
            List<Club> clubOptions;

            if (User.IsInRole(Roles.Advisor))
            {
                var userId = _userManager.GetUserId(User);
                var advisor = await _context.Advisors.FirstOrDefaultAsync(a => a.UserId == userId);
                clubIds = advisor == null ? new List<int>() :
                    await _context.Clubs.Where(c => c.AdvisorId == advisor.AdvisorId).Select(c => c.ClubId).ToListAsync();
                clubOptions = await _context.Clubs.Where(c => clubIds.Contains(c.ClubId)).ToListAsync();
            }
            else
            {
                clubIds = await _context.Clubs.Select(c => c.ClubId).ToListAsync();
                clubOptions = await _context.Clubs.OrderBy(c => c.ClubName).ToListAsync();
            }

            var query = _context.Events.Include(e => e.Club).Where(e => clubIds.Contains(e.ClubId)).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e => e.Title.Contains(search) || e.Club!.ClubName.Contains(search));
            }
            if (clubId.HasValue)
            {
                query = query.Where(e => e.ClubId == clubId.Value);
            }

            var events = await query.ToListAsync();
            if (filter == "Upcoming")
            {
                events = events.Where(e => e.IsUpcoming).ToList();
            }
            else if (filter == "Past")
            {
                events = events.Where(e => !e.IsUpcoming).ToList();
            }

            events = events.OrderBy(e => e.EventDate).ToList();

            ViewData["Filter"] = filter;
            ViewData["ClubFilter"] = clubId;
            ViewData["Search"] = search;
            ViewData["ClubOptions"] = clubOptions;
            ViewData["ReportTitle"] = "Activity Report";
            return View(events);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = $"{Roles.Student},{Roles.Parent}")]
    public class ActivitiesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ActivitiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? filter, int? clubId, string? search)
        {
            var query = _context.Events.Include(e => e.Club).AsQueryable();

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

            var upcoming = events.Where(e => e.IsUpcoming).OrderBy(e => e.EventDate).ThenBy(e => e.StartTime).ToList();
            var past = events.Where(e => !e.IsUpcoming).OrderByDescending(e => e.EventDate).ThenByDescending(e => e.StartTime).ToList();

            ViewData["Filter"] = filter ?? "All";
            ViewData["ClubFilter"] = clubId;
            ViewData["Search"] = search;
            ViewData["ClubOptions"] = await _context.Clubs.OrderBy(c => c.ClubName).ToListAsync();
            ViewData["Upcoming"] = upcoming;
            ViewData["Past"] = past;

            return View();
        }

        public async Task<IActionResult> Details(int id)
        {
            var ev = await _context.Events.Include(e => e.Club).FirstOrDefaultAsync(e => e.EventId == id);
            if (ev == null) return NotFound();
            return View(ev);
        }
    }
}

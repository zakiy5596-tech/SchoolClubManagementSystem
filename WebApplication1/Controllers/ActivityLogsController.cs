using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class ActivityLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ActivityLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, string? action, DateTime? date)
        {
            var query = _context.ActivityLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(l => l.UserName.Contains(search) || l.Description.Contains(search));
            }
            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(l => l.Action == action);
            }
            if (date.HasValue)
            {
                query = query.Where(l => l.Timestamp.Date == date.Value.Date);
            }

            var logs = await query.OrderByDescending(l => l.Timestamp).Take(500).ToListAsync();

            ViewData["Search"] = search;
            ViewData["ActionFilter"] = action;
            ViewData["DateFilter"] = date?.ToString("yyyy-MM-dd");
            ViewData["Actions"] = await _context.ActivityLogs.Select(l => l.Action).Distinct().OrderBy(a => a).ToListAsync();
            ViewData["ReportTitle"] = "Activity Logs";

            return View(logs);
        }
    }
}

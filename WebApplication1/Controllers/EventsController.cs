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
    [Authorize(Roles = $"{Roles.Admin},{Roles.Advisor}")]
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IActivityLogger _logger;

        public EventsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IActivityLogger logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        private async Task<List<int>> GetManageableClubIdsAsync()
        {
            if (User.IsInRole(Roles.Admin))
            {
                return await _context.Clubs.Select(c => c.ClubId).ToListAsync();
            }

            var userId = _userManager.GetUserId(User);
            var advisor = await _context.Advisors.FirstOrDefaultAsync(a => a.UserId == userId);
            if (advisor == null) return new List<int>();

            return await _context.Clubs.Where(c => c.AdvisorId == advisor.AdvisorId).Select(c => c.ClubId).ToListAsync();
        }

        public async Task<IActionResult> Index(string? filter, int? clubId, string? search)
        {
            var manageableClubIds = await GetManageableClubIdsAsync();
            var query = _context.Events.Include(e => e.Club).Where(e => manageableClubIds.Contains(e.ClubId)).AsQueryable();

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

            events = events.OrderBy(e => e.EventDate).ThenBy(e => e.StartTime).ToList();

            ViewData["Filter"] = filter;
            ViewData["ClubFilter"] = clubId;
            ViewData["Search"] = search;
            ViewData["ClubOptions"] = await _context.Clubs.Where(c => manageableClubIds.Contains(c.ClubId)).ToListAsync();
            ViewData["ReportTitle"] = "Activities/Events Report";

            return View(events);
        }

        public async Task<IActionResult> Create()
        {
            var manageableClubIds = await GetManageableClubIdsAsync();
            var model = new EventFormViewModel
            {
                Clubs = await _context.Clubs.Where(c => manageableClubIds.Contains(c.ClubId)).ToListAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventFormViewModel model)
        {
            var manageableClubIds = await GetManageableClubIdsAsync();

            if (!manageableClubIds.Contains(model.ClubId))
            {
                ModelState.AddModelError(nameof(model.ClubId), "You are not authorized to manage this club.");
            }

            if (!ModelState.IsValid)
            {
                model.Clubs = await _context.Clubs.Where(c => manageableClubIds.Contains(c.ClubId)).ToListAsync();
                return View(model);
            }

            var ev = new Event
            {
                ClubId = model.ClubId,
                Title = model.Title,
                Description = model.Description,
                EventDate = model.EventDate,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                Location = model.Location,
                CreatedBy = _userManager.GetUserId(User),
                CreatedAt = DateTime.Now
            };
            _context.Events.Add(ev);
            var saved = await _context.SaveChangesAsync() > 0;

            if (!saved)
            {
                TempData["Error"] = "Unable to save the activity. Please try again.";
                model.Clubs = await _context.Clubs.Where(c => manageableClubIds.Contains(c.ClubId)).ToListAsync();
                return View(model);
            }

            await _logger.LogAsync(_userManager.GetUserId(User), User.Identity!.Name!, "Activity Created", $"Created activity {ev.Title}.");

            TempData["Success"] = "Activity created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var manageableClubIds = await GetManageableClubIdsAsync();
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventId == id);
            if (ev == null) return NotFound();
            if (!manageableClubIds.Contains(ev.ClubId)) return Forbid();

            var model = new EventFormViewModel
            {
                EventId = ev.EventId,
                ClubId = ev.ClubId,
                Title = ev.Title,
                Description = ev.Description,
                EventDate = ev.EventDate,
                StartTime = ev.StartTime,
                EndTime = ev.EndTime,
                Location = ev.Location,
                Clubs = await _context.Clubs.Where(c => manageableClubIds.Contains(c.ClubId)).ToListAsync()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EventFormViewModel model)
        {
            if (id != model.EventId) return NotFound();

            var manageableClubIds = await GetManageableClubIdsAsync();
            if (!manageableClubIds.Contains(model.ClubId))
            {
                ModelState.AddModelError(nameof(model.ClubId), "You are not authorized to manage this club.");
            }

            if (!ModelState.IsValid)
            {
                model.Clubs = await _context.Clubs.Where(c => manageableClubIds.Contains(c.ClubId)).ToListAsync();
                return View(model);
            }

            var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventId == id);
            if (ev == null) return NotFound();
            if (!manageableClubIds.Contains(ev.ClubId)) return Forbid();

            ev.ClubId = model.ClubId;
            ev.Title = model.Title;
            ev.Description = model.Description;
            ev.EventDate = model.EventDate;
            ev.StartTime = model.StartTime;
            ev.EndTime = model.EndTime;
            ev.Location = model.Location;
            ev.UpdatedAt = DateTime.Now;

            var saved = await _context.SaveChangesAsync() >= 0;
            if (!saved)
            {
                TempData["Error"] = "Unable to save the activity. Please try again.";
                model.Clubs = await _context.Clubs.Where(c => manageableClubIds.Contains(c.ClubId)).ToListAsync();
                return View(model);
            }

            await _logger.LogAsync(_userManager.GetUserId(User), User.Identity!.Name!, "Activity Updated", $"Updated activity {ev.Title}.");

            TempData["Success"] = "Activity updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var manageableClubIds = await GetManageableClubIdsAsync();
            var ev = await _context.Events.Include(e => e.Club).FirstOrDefaultAsync(e => e.EventId == id);
            if (ev == null) return NotFound();
            if (!manageableClubIds.Contains(ev.ClubId)) return Forbid();
            return View(ev);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var manageableClubIds = await GetManageableClubIdsAsync();
            var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventId == id);
            if (ev == null) return NotFound();
            if (!manageableClubIds.Contains(ev.ClubId)) return Forbid();

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            var stillExists = await _context.Events.AnyAsync(e => e.EventId == id);
            if (stillExists)
            {
                TempData["Error"] = "Unable to delete the activity. Please try again.";
                return RedirectToAction(nameof(Index));
            }

            await _logger.LogAsync(_userManager.GetUserId(User), User.Identity!.Name!, "Activity Deleted", $"Deleted activity {ev.Title}.");

            TempData["Success"] = "Activity deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}

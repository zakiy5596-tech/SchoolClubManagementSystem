using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class ActivityLogger : IActivityLogger
    {
        private readonly ApplicationDbContext _context;

        public ActivityLogger(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string? userId, string userName, string action, string description)
        {
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                Description = description,
                Timestamp = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }
    }
}

namespace WebApplication1.Models.ViewModels
{
    public class AdvisorDashboardViewModel
    {
        public List<Club> AssignedClubs { get; set; } = new();
        public int TotalMembers { get; set; }
        public int PendingMembershipRequests { get; set; }
        public List<Event> UpcomingActivities { get; set; } = new();
        public List<ActivityLog> RecentLogs { get; set; } = new();
    }
}

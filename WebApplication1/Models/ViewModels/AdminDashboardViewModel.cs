namespace WebApplication1.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalClubs { get; set; }
        public int TotalAdvisors { get; set; }
        public int PendingMembershipRequests { get; set; }
        public int UpcomingActivities { get; set; }
        public List<ActivityLog> RecentLogs { get; set; } = new();
    }
}

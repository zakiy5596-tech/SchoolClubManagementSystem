namespace WebApplication1.Models.ViewModels
{
    public class StudentDashboardViewModel
    {
        public Student Student { get; set; } = null!;
        public List<ClubMembership> MyClubs { get; set; } = new();
        public List<Club> AvailableClubs { get; set; } = new();
        public List<Event> UpcomingActivities { get; set; } = new();
        public List<Event> RecentPastActivities { get; set; } = new();
    }
}

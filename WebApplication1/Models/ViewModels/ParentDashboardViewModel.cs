namespace WebApplication1.Models.ViewModels
{
    public class ParentDashboardViewModel
    {
        public Parent Parent { get; set; } = null!;
        public List<Student> Children { get; set; } = new();
        public List<ClubMembership> ChildMemberships { get; set; } = new();
        public List<Event> UpcomingActivities { get; set; } = new();
    }
}

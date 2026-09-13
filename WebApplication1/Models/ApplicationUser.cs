using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models
{
    public enum UserStatus
    {
        Active,
        Inactive
    }

    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public UserStatus Status { get; set; } = UserStatus.Active;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Student? Student { get; set; }
        public Advisor? Advisor { get; set; }
        public Parent? Parent { get; set; }
    }
}

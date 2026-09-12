using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class ActivityLog
    {
        [Key]
        public int LogId { get; set; }

        public string? UserId { get; set; }

        [StringLength(100)]
        public string UserName { get; set; } = "System";

        [Required, StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string Description { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}

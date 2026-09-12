using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Event
    {
        public int EventId { get; set; }

        [Required]
        public int ClubId { get; set; }

        [ForeignKey(nameof(ClubId))]
        public Club? Club { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required, DataType(DataType.Date)]
        public DateTime EventDate { get; set; }

        [Required, DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required, DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Required, StringLength(200)]
        public string Location { get; set; } = string.Empty;

        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        [NotMapped]
        public DateTime EventStart => EventDate.Date + StartTime;

        [NotMapped]
        public bool IsUpcoming => EventStart >= DateTime.Now;
    }
}

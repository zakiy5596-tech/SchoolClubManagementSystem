using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.ViewModels
{
    public class EventFormViewModel : IValidatableObject
    {
        public int? EventId { get; set; }

        [Required(ErrorMessage = "Club is required.")]
        public int ClubId { get; set; }

        public List<Club> Clubs { get; set; } = new();

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date is required.")]
        [DataType(DataType.Date)]
        public DateTime EventDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Start time is required.")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "End time is required.")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        public bool IsEdit => EventId.HasValue;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (EndTime <= StartTime)
            {
                yield return new ValidationResult("End time cannot be earlier than or equal to start time.", new[] { nameof(EndTime) });
            }
        }
    }
}
